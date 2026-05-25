using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(ProjectId), "projectId")]
public sealed partial class TasksViewModel : BaseViewModel
{
    private readonly PysApi _api;

    public ObservableCollection<TaskItem> TodoColumn { get; } = new();
    public ObservableCollection<TaskItem> InProgressColumn { get; } = new();
    public ObservableCollection<TaskItem> InReviewColumn { get; } = new();
    public ObservableCollection<TaskItem> DoneColumn { get; } = new();
    public ObservableCollection<TaskItem> BlockedColumn { get; } = new();

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private int _projectId;

    [ObservableProperty]
    private string _projectName = "Görevler";

    private TaskItem? _draggedTask;

    public TasksViewModel(PysApi api) => _api = api;

    partial void OnProjectIdChanged(int value) => FireAndForget(LoadAsync);

    [RelayCommand]
    private void DragStarted(TaskItem item) => _draggedTask = item;

    [RelayCommand]
    private async Task DropAsync(string targetStatus)
    {
        var item = _draggedTask;
        _draggedTask = null;
        if (item is null || string.IsNullOrEmpty(targetStatus)) return;
        if (!Enum.TryParse<TaskStatusEnum>(targetStatus, out var newStatus)) return;
        if (item.Status == newStatus) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await _api.UpdateTaskAsync(item.Id, new UpdateTaskRequest(
                item.Title, item.Description, newStatus, item.Priority, item.DueDate, item.AssigneeId));
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || ProjectId == 0) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var data = await _api.GetTasksAsync(ProjectId);
            ProjectName = data.FirstOrDefault()?.ProjectName ?? $"Proje #{ProjectId}";

            TodoColumn.Clear();
            InProgressColumn.Clear();
            InReviewColumn.Clear();
            DoneColumn.Clear();
            BlockedColumn.Clear();

            foreach (var t in data)
            {
                var bucket = t.Status switch
                {
                    TaskStatusEnum.Todo => TodoColumn,
                    TaskStatusEnum.InProgress => InProgressColumn,
                    TaskStatusEnum.InReview => InReviewColumn,
                    TaskStatusEnum.Done => DoneColumn,
                    TaskStatusEnum.Blocked => BlockedColumn,
                    _ => TodoColumn
                };
                bucket.Add(t);
            }
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    private Task NewTaskInStatusAsync(string status)
    {
        var initialStatus = Enum.TryParse<TaskStatusEnum>(status, out var s) ? (int)s : 0;
        return Shell.Current.GoToAsync($"task-edit?projectId={ProjectId}&initialStatus={initialStatus}");
    }

    [RelayCommand]
    private static Task OpenTaskAsync(TaskItem item)
        => item is null ? Task.CompletedTask : Shell.Current.GoToAsync($"task-edit?id={item.Id}&projectId={item.ProjectId}");

    [RelayCommand]
    private async Task DeleteTaskAsync(TaskItem item)
    {
        if (item is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Sil",
            $"'{item.Title}' görevini silmek istiyor musunuz?", "Evet", "Hayır");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            await _api.DeleteTaskAsync(item.Id);
            RemoveTaskLocally(item);
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    private void RemoveTaskLocally(TaskItem item)
    {
        TodoColumn.Remove(item);
        InProgressColumn.Remove(item);
        InReviewColumn.Remove(item);
        DoneColumn.Remove(item);
        BlockedColumn.Remove(item);
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
