namespace PYS.Core.Abstractions;

/// <summary>
/// Sunucu tarafı dosya depolama soyutlaması. Service katmanı fiziksel yol/wwwroot bilmeden
/// dosya saklar; somut implementasyon (ör. <c>PhysicalFileStorage</c>) sunum/host katmanındadır.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// İçeriği <paramref name="category"/> klasörü altında saklar ve istemcinin erişebileceği
    /// göreli URL döner (ör. <c>/avatars/ab12.png</c>).
    /// </summary>
    Task<string> SaveAsync(string category, string originalFileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Daha önce <see cref="SaveAsync"/>'in döndürdüğü göreli URL ile dosyayı siler.</summary>
    Task DeleteAsync(string? relativeUrl, CancellationToken cancellationToken = default);

    /// <summary>Okuma için açar; yoksa <c>null</c> döner. (indirme senaryoları için)</summary>
    FileContent? Open(string? relativeUrl);
}

/// <summary>Bir depolanmış dosyanın okunabilir akışı + meta verisi.</summary>
public sealed record FileContent(Stream Stream, string ContentType, string FileName);
