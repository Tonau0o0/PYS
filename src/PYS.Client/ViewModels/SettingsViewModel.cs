using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

public sealed partial class SettingsViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly AuthState _auth;

    [ObservableProperty]
    private string _email;

    [ObservableProperty]
    private string? _successMessage;

    public string UserName => _auth.Current?.UserName ?? string.Empty;

    public SettingsViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
        _email = auth.Current?.Email ?? string.Empty;
    }

    [RelayCommand]
    private async Task ChangeEmailAsync()
    {
        var email = Email?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorMessage = "Geçerli bir e-posta gir.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            IsBusy = true;
            var updated = await _api.UpdateEmailAsync(email);
            _auth.Set(updated);
            SuccessMessage = "E-postan güncellendi.";
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private static Task ChangePasswordAsync() => Shell.Current.GoToAsync("change-password");

    [RelayCommand]
    private static Task ProfileAsync() => Shell.Current.GoToAsync("profile");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Clear();
        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
