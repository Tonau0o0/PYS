using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(ProjectId), "projectId")]
public sealed partial class MembersViewModel : BaseViewModel
{
    private readonly PysApi _api;

    public MembersViewModel(PysApi api) => _api = api;

    public ObservableCollection<ProjectMemberItem> Members { get; } = new();
    public ObservableCollection<InvitationItem> Invitations { get; } = new();

    [ObservableProperty]
    private int _projectId;

    [ObservableProperty]
    private string _projectName = string.Empty;

    [ObservableProperty]
    private bool _isOwner;

    [ObservableProperty]
    private string _inviteEmail = string.Empty;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isRefreshing;

    partial void OnProjectIdChanged(int value) => FireAndForget(LoadAsync);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy || ProjectId == 0) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var project = await _api.GetProjectAsync(ProjectId);
            ProjectName = project.Name;
            IsOwner = project.IsOwner;

            var members = await _api.GetMembersAsync(ProjectId);
            Members.Clear();
            foreach (var m in members) Members.Add(m);

            if (IsOwner)
            {
                var invs = await _api.GetInvitationsAsync(ProjectId);
                Invitations.Clear();
                foreach (var i in invs) Invitations.Add(i);
            }
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task InviteAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var email = (InviteEmail ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
        {
            ErrorMessage = "Geçerli bir e-posta adresi girin.";
            return;
        }

        try
        {
            IsBusy = true;
            await _api.InviteAsync(ProjectId, new InviteRequest(email));
            SuccessMessage = $"{email} davet edildi.";
            InviteEmail = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveMemberAsync(ProjectMemberItem item)
    {
        if (item is null || !IsOwner) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Çıkar",
            $"{item.UserName} kullanıcısını projeden çıkarmak istiyor musunuz?", "Evet", "Hayır");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            await _api.RemoveMemberAsync(ProjectId, item.UserId);
            Members.Remove(item);
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CancelInvitationAsync(InvitationItem item)
    {
        if (item is null || !IsOwner) return;
        try
        {
            IsBusy = true;
            await _api.CancelInvitationAsync(ProjectId, item.Id);
            Invitations.Remove(item);
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
