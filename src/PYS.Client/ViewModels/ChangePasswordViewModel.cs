using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

public sealed partial class ChangePasswordViewModel : BaseViewModel
{
    private readonly PysApi _api;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _successMessage;

    public ChangePasswordViewModel(PysApi api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(CurrentPassword) || CurrentPassword.Length < 6) errors.Add("Mevcut şifre en az 6 karakter.");
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6) errors.Add("Yeni şifre en az 6 karakter.");
        if (NewPassword != ConfirmPassword) errors.Add("Yeni şifreler eşleşmiyor.");
        if (CurrentPassword == NewPassword) errors.Add("Yeni şifre mevcuttan farklı olmalı.");

        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
            return;
        }

        try
        {
            IsBusy = true;
            await _api.ChangePasswordAsync(new ChangePasswordRequest(CurrentPassword, NewPassword));
            SuccessMessage = "Şifre başarıyla güncellendi.";
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private static Task CancelAsync() => Shell.Current.GoToAsync("..");
}
