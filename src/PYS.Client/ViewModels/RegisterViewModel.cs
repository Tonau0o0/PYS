using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

public sealed partial class RegisterViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly AuthState _auth;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    public RegisterViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorMessage = null;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(UserName) || UserName.Length < 3) errors.Add("Kullanıcı adı en az 3 karakter.");
        if (string.IsNullOrWhiteSpace(Email) || !new EmailAddressAttribute().IsValid(Email)) errors.Add("Geçerli bir e-posta girin.");
        if (string.IsNullOrWhiteSpace(FullName) || FullName.Length < 2) errors.Add("Ad soyad en az 2 karakter.");
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6) errors.Add("Şifre en az 6 karakter.");
        if (Password != ConfirmPassword) errors.Add("Şifreler eşleşmiyor.");
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
            return;
        }

        try
        {
            IsBusy = true;
            var res = await _api.RegisterAsync(new RegisterRequest(UserName.Trim(), Email.Trim(), FullName.Trim(), Password));
            _auth.Set(res);
            await Shell.Current.GoToAsync("//projects");
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
