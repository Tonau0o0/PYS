using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;
using PYS.Core.Common;

namespace PYS.Client.ViewModels;

public sealed partial class ProjectsViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly AuthState _auth;

    public ObservableCollection<ProjectItem> Projects { get; } = new();

    public IReadOnlyList<ProjectStatus?> StatusFilters { get; } = new ProjectStatus?[]
    {
        null,
        ProjectStatus.Planned,
        ProjectStatus.InProgress,
        ProjectStatus.OnHold,
        ProjectStatus.Completed,
        ProjectStatus.Cancelled
    };

    [ObservableProperty]
    private ProjectStatus? _selectedStatus;

    [ObservableProperty]
    private bool _isRefreshing;

    public string GreetingText => _auth.Current is null ? string.Empty : $"Merhaba {_auth.Current.FullName}";

    public ProjectsViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
    }

    partial void OnSelectedStatusChanged(ProjectStatus? value) => FireAndForget(LoadAsync);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var data = await _api.GetProjectsAsync((int?)SelectedStatus);
            Projects.Clear();
            foreach (var p in data) Projects.Add(p);
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    private static Task NewProjectAsync() => Shell.Current.GoToAsync("project-edit");

    [RelayCommand]
    private static Task OpenProjectAsync(ProjectItem item)
        => item is null ? Task.CompletedTask : Shell.Current.GoToAsync($"tasks?projectId={item.Id}");

    [RelayCommand]
    private static Task EditProjectAsync(ProjectItem item)
        => item is null ? Task.CompletedTask : Shell.Current.GoToAsync($"project-edit?id={item.Id}");

    [RelayCommand]
    private static Task ManageMembersAsync(ProjectItem item)
        => item is null ? Task.CompletedTask : Shell.Current.GoToAsync($"members?projectId={item.Id}");

    [RelayCommand]
    private async Task DeleteProjectAsync(ProjectItem item)
    {
        if (item is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Sil",
            $"'{item.Name}' projesini silmek istiyor musunuz?", "Evet", "Hayır");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            await _api.DeleteProjectAsync(item.Id);
            Projects.Remove(item);
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Clear();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private static Task ChangePasswordAsync() => Shell.Current.GoToAsync("change-password");

    [RelayCommand]
    private static Task ProfileAsync() => Shell.Current.GoToAsync("profile");
}
