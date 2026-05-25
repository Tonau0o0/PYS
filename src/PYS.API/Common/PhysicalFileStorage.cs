using Microsoft.AspNetCore.StaticFiles;
using PYS.Core.Abstractions;

namespace PYS.API.Common;

/// <summary>
/// <see cref="IFileStorage"/>'in wwwroot tabanlı somut implementasyonu. Dosyalar
/// <c>{WebRoot}/{category}/{guid}{ext}</c> altına yazılır ve statik dosya middleware'i
/// ile <c>/{category}/{guid}{ext}</c> yolundan servis edilir.
/// </summary>
public sealed class PhysicalFileStorage : IFileStorage
{
    private readonly string _webRoot;
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public PhysicalFileStorage(IWebHostEnvironment env)
    {
        // Bazı şablonlarda WebRootPath null olabilir; ContentRoot/wwwroot'a düşürüyoruz.
        _webRoot = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
    }

    public async Task<string> SaveAsync(string category, string originalFileName, Stream content, CancellationToken cancellationToken = default)
    {
        var safeCategory = SanitizeSegment(category);
        var ext = Path.GetExtension(originalFileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";

        var dir = Path.Combine(_webRoot, safeCategory);
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, fileName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        return $"/{safeCategory}/{fileName}";
    }

    public Task DeleteAsync(string? relativeUrl, CancellationToken cancellationToken = default)
    {
        var full = ResolveExisting(relativeUrl);
        if (full is not null)
        {
            try { File.Delete(full); } catch { /* best-effort */ }
        }
        return Task.CompletedTask;
    }

    public FileContent? Open(string? relativeUrl)
    {
        var full = ResolveExisting(relativeUrl);
        if (full is null) return null;

        if (!ContentTypes.TryGetContentType(full, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
        return new FileContent(stream, contentType, Path.GetFileName(full));
    }

    /// <summary>Göreli URL'i fiziksel yola çevirir; path-traversal'a karşı _webRoot içinde kalmayı zorlar.</summary>
    private string? ResolveExisting(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return null;

        var relative = relativeUrl.TrimStart('/', '\\');
        var full = Path.GetFullPath(Path.Combine(_webRoot, relative));

        var rootFull = Path.GetFullPath(_webRoot);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) return null;

        return File.Exists(full) ? full : null;
    }

    private static string SanitizeSegment(string segment)
    {
        var cleaned = new string(segment.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '/').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "files" : cleaned.Trim('/');
    }
}
