using PYS.Client.Services;

namespace PYS.Client;

/// <summary>Windows native klasör seçici (FolderPicker). WinRT pencere handle'ı ile başlatılır.</summary>
public sealed class FolderPickerService : IFolderPicker
{
    public async Task<string?> PickFolderAsync()
    {
        var picker = new global::Windows.Storage.Pickers.FolderPicker();
        picker.FileTypeFilter.Add("*");

        var mauiWindow = Application.Current?.Windows.FirstOrDefault();
        var nativeWindow = mauiWindow?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
