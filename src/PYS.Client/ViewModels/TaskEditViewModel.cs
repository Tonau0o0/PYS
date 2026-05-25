using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using PYS.Client.Models;
using PYS.Client.Services;
using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(TaskIdQuery), "id")]
[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(InitialStatus), "initialStatus")]
public sealed partial class TaskEditViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly HttpClient _http;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _membersLoaded;
    private bool _taskLoaded;

    /// <summary>Bu göreve bağlı proje kaynakları (tüm üyelerce görülür).</summary>
    public ObservableCollection<ProjectResourceItem> LinkedResources { get; } = new();

    public bool IsExistingTask => TaskId is not null;

    public TaskEditViewModel(PysApi api, HttpClient http)
    {
        _api = api;
        _http = http;
    }

    [ObservableProperty]
    private int? _taskId;

    /// <summary>
    /// Shell route parametresi <c>id</c> string olarak gelir. Doğrudan <see cref="TaskId"/>
    /// (int?) hedeflemek MAUI'de <see cref="InvalidCastException"/> fırlatır (string→Nullable&lt;int&gt;
    /// dönüşümü desteklenmez). Burada güvenli parse edip TaskId'ye aktarıyoruz.
    /// </summary>
    public string? TaskIdQuery
    {
        set => TaskId = int.TryParse(value, out var id) ? id : null;
    }

    [ObservableProperty]
    private int _projectId;

    [ObservableProperty]
    private int _initialStatus;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TaskStatusEnum _status = TaskStatusEnum.Todo;

    [ObservableProperty]
    private TaskPriority _priority = TaskPriority.Medium;

    [ObservableProperty]
    private DateTime _dueDate = DateTime.Today.AddDays(7);

    [ObservableProperty]
    private bool _hasDueDate = true;

    [ObservableProperty]
    private bool _isDone;

    [ObservableProperty]
    private ProjectMemberItem? _selectedAssignee;

    public ObservableCollection<ProjectMemberItem> ProjectMembers { get; } = new();

    public IReadOnlyList<TaskStatusEnum> StatusOptions { get; } = Enum.GetValues<TaskStatusEnum>();
    public IReadOnlyList<TaskPriority> PriorityOptions { get; } = Enum.GetValues<TaskPriority>();

    public string PageTitle => TaskId is null ? "Yeni Görev" : "Görev Düzenle";

    partial void OnTaskIdChanged(int? value)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsExistingTask));
        _taskLoaded = false;
        FireAndForget(LoadAsync);
    }

    partial void OnProjectIdChanged(int value) => FireAndForget(LoadAsync);

    partial void OnInitialStatusChanged(int value)
    {
        if (TaskId is null && Enum.IsDefined(typeof(TaskStatusEnum), value))
        {
            Status = (TaskStatusEnum)value;
        }
    }

    partial void OnIsDoneChanged(bool value)
    {
        if (value) Status = TaskStatusEnum.Done;
        else if (Status == TaskStatusEnum.Done) Status = TaskStatusEnum.Todo;
    }

    public async Task LoadAsync()
    {
        if (ProjectId == 0) return;            // Guard: query parametreleri henüz hazır değil

        await _gate.WaitAsync();               // Çift tetikleme yarışını serialize et (skip etme, sıraya al)
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            // 1) VERİ ÇEK — UI'a dokunmaz, continuation hangi thread'de olursa olsun güvenli
            IReadOnlyList<ProjectMemberItem>? members =
                _membersLoaded ? null : await _api.GetMembersAsync(ProjectId);

            TaskItem? task = null;
            if (TaskId is not null && !_taskLoaded)
            {
                var tasks = await _api.GetTasksAsync(ProjectId);
                task = tasks.FirstOrDefault(x => x.Id == TaskId.Value);
                if (task is null)
                {
                    ErrorMessage = "Görev bulunamadı.";
                    return;
                }
            }

            // 2) UI GÜNCELLE — ObservableCollection + bound property'ler yalnız main thread'de (SKILL #4)
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (members is not null)
                {
                    ProjectMembers.Clear();
                    foreach (var m in members) ProjectMembers.Add(m);
                    _membersLoaded = true;
                }

                if (task is not null)
                {
                    Title = task.Title;
                    Description = task.Description ?? string.Empty;
                    Status = task.Status;
                    Priority = task.Priority;
                    HasDueDate = task.DueDate.HasValue;
                    DueDate = task.DueDate ?? DateTime.Today.AddDays(7);
                    IsDone = task.Status == TaskStatusEnum.Done;
                    SelectedAssignee = ProjectMembers.FirstOrDefault(m => m.UserId == task.AssigneeId);
                    _taskLoaded = true;
                }
            });

            // Göreve bağlı kaynakları her yüklemede tazele (drag ile eklenmiş olabilir).
            if (TaskId is not null)
            {
                var linked = await _api.GetTaskResourcesAsync(TaskId.Value);
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LinkedResources.Clear();
                    foreach (var r in linked) LinkedResources.Add(r);
                });
            }
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; _gate.Release(); }
    }

    [RelayCommand]
    private async Task OpenLinkedResourceAsync(ProjectResourceItem item)
    {
        if (item is null) return;

        if (item.IsYouTube && !string.IsNullOrEmpty(item.YouTubeId))
        {
            await Shell.Current.GoToAsync($"video-player?videoId={item.YouTubeId}");
            return;
        }

        if (!item.IsFile || string.IsNullOrEmpty(item.Url)) return;

        try
        {
            IsBusy = true;
            var bytes = await _http.GetByteArrayAsync($"{MauiProgram.ApiBaseUrl}{item.Url}");
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloads);
            var target = Path.Combine(downloads, item.FileName ?? $"resource_{item.Id}");
            await File.WriteAllBytesAsync(target, bytes);

            var open = await Shell.Current.DisplayAlertAsync("İndirildi", target, "Aç", "Tamam");
            if (open) await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(target) });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveLinkedResourceAsync(ProjectResourceItem item)
    {
        if (item is null || TaskId is null) return;
        try
        {
            await _api.UnlinkResourceFromTaskAsync(TaskId.Value, item.Id);
            await MainThread.InvokeOnMainThreadAsync(() => LinkedResources.Remove(item));
        }
        catch (Exception ex) { HandleException(ex); }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Title) || Title.Trim().Length < 2) errors.Add("Başlık en az 2 karakter olmalı.");
        if (ProjectId == 0) errors.Add("Proje ID eksik.");
        if (errors.Count > 0) { ErrorMessage = string.Join(" ", errors); return; }

        try
        {
            IsBusy = true;
            DateTime? due = HasDueDate ? DueDate : null;
            int? assigneeId = SelectedAssignee?.UserId;
            var desc = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

            if (TaskId is null)
            {
                await _api.CreateTaskAsync(new CreateTaskRequest(
                    Title.Trim(), desc, Status, Priority, due, ProjectId, assigneeId));
            }
            else
            {
                await _api.UpdateTaskAsync(TaskId.Value, new UpdateTaskRequest(
                    Title.Trim(), desc, Status, Priority, due, assigneeId));
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private static Task CancelAsync() => Shell.Current.GoToAsync("..");
}
