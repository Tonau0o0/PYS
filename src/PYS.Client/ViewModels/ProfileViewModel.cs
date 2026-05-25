using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private string? _successMessage;

    public string DisplayName => _auth.Current?.FullName ?? string.Empty;
    public string UserName => _auth.Current?.UserName ?? string.Empty;

    public ProfileViewModel(PysApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
        Palette = new ObservableCollection<string>(ColorPalette.Defaults);
        _currentColor = auth.Current?.Color ?? "#2196F3";
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
