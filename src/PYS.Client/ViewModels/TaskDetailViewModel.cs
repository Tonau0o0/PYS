using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(IdQuery), "id")]
[QueryProperty(nameof(ProjectId), "projectId")]
public sealed partial class TaskDetailViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly ResourceDownloadService _downloader;
    private int _taskId;

    /// <summary>Bağlı kaynak ağacı (klasörler açılınca çocukları girintili eklenir).</summary>
    public ObservableCollection<ResourceNode> ResourceNodes { get; } = new();
    public ObservableCollection<TaskCommentItem> Comments { get; } = new();

    [ObservableProperty]
    private int _projectId;

    [ObservableProperty]
    private TaskItem? _task;

    [ObservableProperty]
    private string _newComment = string.Empty;

    public TaskDetailViewModel(PysApi api, ResourceDownloadService downloader)
    {
        _api = api;
        _downloader = downloader;
    }

    public string? IdQuery
    {
        set
        {
            if (int.TryParse(value, out var id))
            {
                _taskId = id;
                FireAndForget(LoadAsync);
            }
        }
    }

    public bool HasAssignee => Task?.AssigneeId is not null;
    public string AssigneeName => Task?.AssigneeUserName ?? "Atanmadı";
    public string AssigneeColor => Task?.AssigneeColor ?? "#9E9E9E";
    public string? AssigneeAvatarUrl => Task?.AssigneeAvatarUrl;
    public bool HasAssigneeAvatar => !string.IsNullOrEmpty(Task?.AssigneeAvatarUrl);

    partial void OnTaskChanged(TaskItem? value)
    {
        OnPropertyChanged(nameof(HasAssignee));
        OnPropertyChanged(nameof(AssigneeName));
        OnPropertyChanged(nameof(AssigneeColor));
        OnPropertyChanged(nameof(AssigneeAvatarUrl));
        OnPropertyChanged(nameof(HasAssigneeAvatar));
    }

    public async Task LoadAsync()
    {
        if (_taskId == 0) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var task = await _api.GetTaskAsync(_taskId);
            var resources = await _api.GetTaskResourcesAsync(_taskId);
            var comments = await _api.GetCommentsAsync(_taskId);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Task = task;
                if (ProjectId == 0) ProjectId = task.ProjectId;

                ResourceNodes.Clear();
                foreach (var r in resources) ResourceNodes.Add(new ResourceNode(r, 0));

                Comments.Clear();
                foreach (var c in comments) Comments.Add(c);
            });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task NodeTapAsync(ResourceNode node)
    {
        if (node is null) return;

        if (node.Item.IsFolder) { await ToggleFolderAsync(node); return; }

        if (node.Item.IsYouTube && !string.IsNullOrEmpty(node.Item.YouTubeId))
        {
            await Shell.Current.GoToAsync($"video-player?videoId={node.Item.YouTubeId}");
            return;
        }

        await DownloadNodeAsync(node); // dosya → indir
    }

    private async Task ToggleFolderAsync(ResourceNode node)
    {
        var idx = ResourceNodes.IndexOf(node);
        if (idx < 0) return;

        if (node.IsExpanded)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                int i = idx + 1;
                while (i < ResourceNodes.Count && ResourceNodes[i].Indent > node.Indent)
                    ResourceNodes.RemoveAt(i);
                node.IsExpanded = false;
            });
            return;
        }

        try
        {
            IsBusy = true;
            var children = await _api.GetResourcesAsync(ProjectId, node.Item.Id);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var at = ResourceNodes.IndexOf(node) + 1;
                foreach (var c in children)
                    ResourceNodes.Insert(at++, new ResourceNode(c, node.Indent + 1));
                node.IsExpanded = true;
            });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DownloadNodeAsync(ResourceNode node)
    {
        if (node is null || node.Item.IsYouTube) return;
        try
        {
            IsBusy = true;
            var path = await _downloader.DownloadAsync(ProjectId, node.Item);
            if (path is null) return; // iptal
            var open = await Shell.Current.DisplayAlertAsync("İndirildi", path, "Aç", "Tamam");
            if (open) await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveNodeAsync(ResourceNode node)
    {
        if (node is null || !node.CanUnlink) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Kaldır",
            $"'{node.Item.Title}' bu görevden kaldırılsın mı? (Proje kaynaklarından silinmez)", "Evet", "Hayır");
        if (!confirm) return;

        try
        {
            await _api.UnlinkResourceFromTaskAsync(_taskId, node.Item.Id);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var idx = ResourceNodes.IndexOf(node);
                if (idx < 0) return;
                int i = idx + 1;
                while (i < ResourceNodes.Count && ResourceNodes[i].Indent > node.Indent)
                    ResourceNodes.RemoveAt(i);
                ResourceNodes.RemoveAt(idx);
            });
        }
        catch (Exception ex) { HandleException(ex); }
    }

    [RelayCommand]
    private async Task AddCommentAsync()
    {
        var text = NewComment?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            IsBusy = true;
            var created = await _api.AddCommentAsync(_taskId, text);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Comments.Add(created);
                NewComment = string.Empty;
            });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteCommentAsync(TaskCommentItem item)
    {
        if (item is null) return;
        try
        {
            await _api.DeleteCommentAsync(item.Id);
            await MainThread.InvokeOnMainThreadAsync(() => Comments.Remove(item));
        }
        catch (Exception ex) { HandleException(ex); }
    }

    [RelayCommand]
    private Task EditAsync() => Shell.Current.GoToAsync($"task-edit?id={_taskId}&projectId={ProjectId}");

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var confirm = await Shell.Current.DisplayAlertAsync("Sil",
            $"'{Task?.Title}' görevini silmek istiyor musunuz?", "Evet", "Hayır");
        if (!confirm) return;
        try
        {
            await _api.DeleteTaskAsync(_taskId);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { HandleException(ex); }
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
