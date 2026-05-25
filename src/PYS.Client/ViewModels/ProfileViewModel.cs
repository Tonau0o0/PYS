using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using PYS.Client.Services;
using PYS.Core.Common;

namespace PYS.Client.ViewModels;

public sealed partial class ProfileViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly AuthState _auth;

    public ObservableCollection<string> Palette { get; }

    [ObservableProperty]
    private string _currentColor;

    [ObservableProperty]
    private string _fullName;

    [ObservableProperty]
    private string? _successMessage;

    public string UserName => _auth.Current?.UserName ?? string.Empty;

    public string? AvatarUrl => string.IsNullOrEmpty(_auth.Current?.AvatarUrl)
        ? null
        : MauiProgram.ApiBaseUrl + _auth.Current!.AvatarUrl;

    public bool HasAvatar => !string.IsNullOrEmpty(_auth.Current?.AvatarUrl);

    public ProfileViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
        Palette = new ObservableCollection<string>(ColorPalette.Defaults);
        _currentColor = auth.Current?.Color ?? "#2196F3";
        _fullName = auth.Current?.FullName ?? string.Empty;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var name = FullName?.Trim() ?? string.Empty;
        if (name.Length < 2)
        {
            ErrorMessage = "İsim en az 2 karakter olmalı.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            IsBusy = true;
            var updated = await _api.UpdateProfileAsync(name);
            _auth.Set(updated);
            SuccessMessage = "Profilin güncellendi.";
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PickAvatarAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Profil resmi seç",
                FileTypes = FilePickerFileType.Images
            });
            if (file is null) return;

            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            var updated = await _api.UploadAvatarAsync(stream, file.FileName);
            _auth.Set(updated);

            OnPropertyChanged(nameof(AvatarUrl));
            OnPropertyChanged(nameof(HasAvatar));
            SuccessMessage = "Profil resmin güncellendi.";
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SelectAsync(string color)
    {
        if (string.IsNullOrWhiteSpace(color) || color == CurrentColor) return;

        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            IsBusy = true;
            await _api.UpdateMyColorAsync(color);
            CurrentColor = color;

            if (_auth.Current is not null)
            {
                _auth.Set(_auth.Current with { Color = color });
            }

            SuccessMessage = "Rengin güncellendi.";
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
