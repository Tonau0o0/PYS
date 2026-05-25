using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using PYS.Client.Models;
using PYS.Client.Services;

namespace PYS.Client.ViewModels;

/// <summary>
/// Görevler ekranının alt panelindeki "Proje Kaynakları" — klasörlü dosya sistemi + YouTube.
/// İç içe klasör navigasyonu, sürükle-bırak ile taşıma ve göreve bağlama için sürüklenen
/// kaynağın paylaşımını sağlar.
/// </summary>
public sealed partial class ResourcesViewModel : BaseViewModel
{
    private readonly PysApi _api;
    private readonly ResourceDownloadService _downloader;

    // Bulunduğumuz klasör yolu (boş = kök).
    private readonly List<(int Id, string Name)> _trail = new();

    public ObservableCollection<ProjectResourceItem> Resources { get; } = new();

    [ObservableProperty]
    private int _projectId;

    /// <summary>Şu an sürüklenen kaynak (klasöre taşıma veya göreve bağlama için).</summary>
    public ProjectResourceItem? DraggedResource { get; private set; }

    public int? CurrentFolderId => _trail.Count > 0 ? _trail[^1].Id : null;
    public bool IsInSubfolder => _trail.Count > 0;
    public string CurrentPathText => _trail.Count == 0 ? "Kök" : "Kök / " + string.Join(" / ", _trail.Select(t => t.Name));

    public ResourcesViewModel(PysApi api, ResourceDownloadService downloader)
    {
        _api = api;
        _downloader = downloader;
    }

    public async Task LoadAsync()
    {
        if (ProjectId == 0) return;
        try
        {
            var data = await _api.GetResourcesAsync(ProjectId, CurrentFolderId);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Resources.Clear();
                foreach (var r in data) Resources.Add(r);
            });
        }
        catch (Exception ex) { HandleException(ex); }
    }

    private void NotifyPath()
    {
        OnPropertyChanged(nameof(CurrentFolderId));
        OnPropertyChanged(nameof(IsInSubfolder));
        OnPropertyChanged(nameof(CurrentPathText));
    }

    [RelayCommand]
    private async Task NewFolderAsync()
    {
        try
        {
            var name = await Shell.Current.DisplayPromptAsync("Yeni Klasör", "Klasör adı:", "Oluştur", "İptal");
            if (string.IsNullOrWhiteSpace(name)) return;

            IsBusy = true;
            await _api.CreateFolderAsync(ProjectId, name.Trim(), CurrentFolderId);
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddFileAsync()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Proje dosyası seç" });
            if (file is null) return;

            var title = await Shell.Current.DisplayPromptAsync(
                "Başlık", "Dosya başlığı (boş = dosya adı):", "Ekle", "İptal",
                initialValue: Path.GetFileNameWithoutExtension(file.FileName));
            if (title is null) return;

            IsBusy = true;
            await using var stream = await file.OpenReadAsync();
            await _api.UploadResourceAsync(ProjectId, stream, file.FileName,
                string.IsNullOrWhiteSpace(title) ? file.FileName : title.Trim(), CurrentFolderId);
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
            await _api.AddYouTubeAsync(ProjectId,
                string.IsNullOrWhiteSpace(title) ? "YouTube Videosu" : title.Trim(), url.Trim(), CurrentFolderId);
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task OpenResourceAsync(ProjectResourceItem item)
    {
        if (item is null) return;

        if (item.IsFolder)
        {
            _trail.Add((item.Id, item.Title));
            NotifyPath();
            await LoadAsync();
            return;
        }

        if (item.IsYouTube && !string.IsNullOrEmpty(item.YouTubeId))
        {
            await Shell.Current.GoToAsync($"video-player?videoId={item.YouTubeId}");
            return;
        }

        await DownloadFileAsync(item);
    }

    [RelayCommand]
    private async Task GoUpAsync()
    {
        if (_trail.Count == 0) return;
        _trail.RemoveAt(_trail.Count - 1);
        NotifyPath();
        await LoadAsync();
    }

    [RelayCommand]
    private void DragStarting(ProjectResourceItem item) => DraggedResource = item;

    /// <summary>Bir kaynağı bir klasör kartının üzerine bırakınca o klasöre taşır.</summary>
    [RelayCommand]
    private async Task DropOnFolderAsync(ProjectResourceItem folder)
    {
        var dragged = DraggedResource;
        DraggedResource = null;
        if (dragged is null || folder is null || !folder.IsFolder || dragged.Id == folder.Id) return;

        try
        {
            IsBusy = true;
            await _api.MoveResourceAsync(ProjectId, dragged.Id, folder.Id);
            await LoadAsync();
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    private async Task DownloadFileAsync(ProjectResourceItem item)
    {
        try
        {
            IsBusy = true;
            var path = await _downloader.DownloadAsync(ProjectId, item);
            if (path is null) return; // iptal
            var open = await Shell.Current.DisplayAlertAsync("İndirildi", path, "Aç", "Tamam");
            if (open) await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(path) });
        }
        catch (Exception ex) { HandleException(ex); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteResourceAsync(ProjectResourceItem item)
    {
        if (item is null) return;
        var msg = item.IsFolder
            ? $"'{item.Title}' klasörünü ve içindeki her şeyi silmek istiyor musunuz?"
            : $"'{item.Title}' kaynağını silmek istiyor musunuz?";
        var confirm = await Shell.Current.DisplayAlertAsync("Sil", msg, "Evet", "Hayır");
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
