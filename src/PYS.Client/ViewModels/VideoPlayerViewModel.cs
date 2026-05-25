using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PYS.Client.ViewModels;

[QueryProperty(nameof(VideoId), "videoId")]
public sealed partial class VideoPlayerViewModel : BaseViewModel
{
    [ObservableProperty]
    private string? _videoId;

    [ObservableProperty]
    private string? _embedUrl;

    partial void OnVideoIdChanged(string? value)
    {
        // /embed/ yoluna doğrudan navigasyon WebView'da "Hata 153" (yapılandırma) verir.
        // Tam izleme URL'i WebView2'de güvenilir şekilde oynar.
        EmbedUrl = string.IsNullOrEmpty(value)
            ? null
            : $"https://www.youtube.com/watch?v={value}";
    }

    [RelayCommand]
    private static Task BackAsync() => Shell.Current.GoToAsync("..");
}
