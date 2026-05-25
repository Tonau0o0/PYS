using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly AuthState _auth;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = null;
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kullanıcı adı ve şifre zorunlu.";
            return;
        }
        if (Password.Length < 6)
        {
            ErrorMessage = "Şifre en az 6 karakter olmalı.";
            return;
        }

        try
        {
            IsBusy = true;
            var res = await _api.LoginAsync(new LoginRequest(UserName.Trim(), Password));
            _auth.Set(res);
            UserName = string.Empty;
            Password = string.Empty;
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
    private static Task GoRegisterAsync() => Shell.Current.GoToAsync("register");
}
