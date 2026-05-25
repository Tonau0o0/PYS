using PYS.Client.Models;

namespace PYS.Client.Services;

/// <summary>
/// Bir kaynağı (dosya veya klasör→zip) kullanıcının seçtiği klasöre indirir.
/// Birden çok ViewModel tarafından paylaşılır (DRY).
/// </summary>
public sealed class ResourceDownloadService
{
    private readonly PysApi _api;
    private readonly IFolderPicker _folderPicker;

    public ResourceDownloadService(PysApi api, IFolderPicker folderPicker)
    {
        _api = api;
        _folderPicker = folderPicker;
    }

    /// <summary>Hedef klasörü seçtirir, indirir ve yazar. Kaydedilen yolu döner; iptalde null.</summary>
    public async Task<string?> DownloadAsync(int projectId, ProjectResourceItem item)
    {
        var fileName = item.IsFolder ? $"{item.Title}.zip" : (item.FileName ?? item.Title);

        var folder = await _folderPicker.PickFolderAsync();
        if (string.IsNullOrEmpty(folder)) return null; // iptal

        var bytes = await _api.DownloadResourceBytesAsync(projectId, item.Id);
        var target = Path.Combine(folder, Sanitize(fileName));
        await File.WriteAllBytesAsync(target, bytes);
        return target;
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "indirilen_dosya" : name;
    }
}
