using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

/// <summary>
/// Görevler ekranının alt panelindeki "Proje Kaynakları" (dosyalar + YouTube videoları).
/// TasksViewModel bunu bir alt-VM olarak barındırır (SRP).
/// </summary>
public sealed partial class ResourcesViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly HttpClient _http;

    public ObservableCollection<ProjectResourceItem> Resources { get; } = new();

    [ObservableProperty]
    private int _projectId;

    public ResourcesViewModel(PysApi api, HttpClient http)
    {
        _api = api;
        _http = http;
    }

    public async Task LoadAsync()
    {
        if (ProjectId == 0) return;
        try
        {
            var data = await _api.GetResourcesAsync(ProjectId);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Resources.Clear();
                foreach (var r in data) Resources.Add(r);
            });
        }
        catch (Exception ex) { HandleException(ex); }
    }

    [RelayCommand]
    private async Task AddFileAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Proje dosyası seç" });
            if (file is null) return;

            var title = await Shell.Current.DisplayPromptAsync(
                "Başlık", "Bu dosya için bir başlık gir (boş bırakırsan dosya adı kullanılır):",
                "Ekle", "İptal", initialValue: Path.GetFileNameWithoutExtension(file.FileName));
            if (title is null) return; // İptal

            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            await _api.UploadResourceAsync(ProjectId, stream, file.FileName,
                string.IsNullOrWhiteSpace(title) ? file.FileName : title.Trim());
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddYouTubeAsync()
    {
        try
        {
            var url = await Shell.Current.DisplayPromptAsync(
                "YouTube Linki", "Video URL'sini yapıştır:", "Devam", "İptal",
                placeholder: "https://www.youtube.com/watch?v=...");
            if (string.IsNullOrWhiteSpace(url)) return;

            var title = await Shell.Current.DisplayPromptAsync(
                "Başlık", "Video başlığı:", "Ekle", "İptal", initialValue: "YouTube Videosu");
            if (title is null) return;

            IsBusy = true;
            await _api.AddYouTubeAsync(ProjectId, string.IsNullOrWhiteSpace(title) ? "YouTube Videosu" : title.Trim(), url.Trim());
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task OpenResourceAsync(ProjectResourceItem item)
    {
        if (item is null) return;

        if (item.IsYouTube && !string.IsNullOrEmpty(item.YouTubeId))
        {
            await Shell.Current.GoToAsync($"video-player?videoId={item.YouTubeId}");
            return;
        }

        await DownloadFileAsync(item);
    }

    private async Task DownloadFileAsync(ProjectResourceItem item)
    {
        if (string.IsNullOrEmpty(item.Url)) return;
        try
        {
            IsBusy = true;
            var bytes = await _http.GetByteArrayAsync($"{MauiProgram.ApiBaseUrl}{item.Url}");

            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloads);
            var target = Path.Combine(downloads, item.FileName ?? $"resource_{item.Id}");
            await File.WriteAllBytesAsync(target, bytes);

            var open = await Shell.Current.DisplayAlertAsync("İndirildi", target, "Aç", "Tamam");
            if (open) await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(target) });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteResourceAsync(ProjectResourceItem item)
    {
        if (item is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Sil",
            $"'{item.Title}' kaynağını silmek istiyor musunuz?", "Evet", "Hayır");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            await _api.DeleteResourceAsync(ProjectId, item.Id);
            await MainThread.InvokeOnMainThreadAsync(() => Resources.Remove(item));
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }
}
