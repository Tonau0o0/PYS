using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;
using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(TaskId), "id")]
[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(InitialStatus), "initialStatus")]
public sealed partial class TaskEditViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private bool _membersLoaded;
    private bool _taskLoaded;

    public TaskEditViewModel(PysApi api) => _api = api;

    [ObservableProperty]
    private int? _taskId;

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
        if (ProjectId == 0) return;

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (!_membersLoaded)
            {
                var members = await _api.GetMembersAsync(ProjectId);
                ProjectMembers.Clear();
                foreach (var m in members) ProjectMembers.Add(m);
                _membersLoaded = true;
            }

            if (TaskId is not null && !_taskLoaded)
            {
                var tasks = await _api.GetTasksAsync(ProjectId);
                var t = tasks.FirstOrDefault(x => x.Id == TaskId.Value);
                if (t is null)
                {
                    ErrorMessage = "Görev bulunamadı.";
                    return;
                }

                Title = t.Title;
                Description = t.Description ?? string.Empty;
                Status = t.Status;
                Priority = t.Priority;
                HasDueDate = t.DueDate.HasValue;
                DueDate = t.DueDate ?? DateTime.Today.AddDays(7);
                IsDone = t.Status == TaskStatusEnum.Done;
                SelectedAssignee = ProjectMembers.FirstOrDefault(m => m.UserId == t.AssigneeId);
                _taskLoaded = true;
            }
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
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
