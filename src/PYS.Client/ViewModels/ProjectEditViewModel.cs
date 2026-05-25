using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;
using PYS.Core.Common;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(ProjectId), "id")]
public sealed partial class ProjectEditViewModel : BaseViewModel
{
    private readonly PysApi _api;

    public ProjectEditViewModel(PysApi api) => _api = api;

    [ObservableProperty]
    private int? _projectId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ProjectStatus _status = ProjectStatus.Planned;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today.AddDays(30);

    [ObservableProperty]
    private bool _hasEndDate;

    public IReadOnlyList<ProjectStatus> StatusOptions { get; } = Enum.GetValues<ProjectStatus>();

    public string PageTitle => ProjectId is null ? "Yeni Proje" : "Proje Düzenle";

    partial void OnProjectIdChanged(int? value)
    {
        OnPropertyChanged(nameof(PageTitle));
        FireAndForget(LoadAsync);
    }

    public async Task LoadAsync()
    {
        if (ProjectId is null) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var p = await _api.GetProjectAsync(ProjectId.Value);
            Name = p.Name;
            Description = p.Description ?? string.Empty;
            Status = p.Status;
            StartDate = p.StartDate;
            HasEndDate = p.EndDate.HasValue;
            EndDate = p.EndDate ?? DateTime.Today;
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name) || Name.Length < 2) errors.Add("Proje adı en az 2 karakter olmalı.");
        if (HasEndDate && EndDate.Date < StartDate.Date) errors.Add("Bitiş tarihi başlangıçtan önce olamaz.");
        if (errors.Count > 0) { ErrorMessage = string.Join(" ", errors); return; }

        try
        {
            IsBusy = true;
            DateTime? endDateValue = HasEndDate ? EndDate : null;
            var desc = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

            if (ProjectId is null)
            {
                await _api.CreateProjectAsync(new CreateProjectRequest(Name.Trim(), desc, Status, StartDate, endDateValue));
            }
            else
            {
                await _api.UpdateProjectAsync(ProjectId.Value, new UpdateProjectRequest(Name.Trim(), desc, Status, StartDate, endDateValue));
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private static Task CancelAsync() => Shell.Current.GoToAsync("..");
}
