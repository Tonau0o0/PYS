using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Core.Entities;

/// <summary>
/// Bir projeye iliştirilen paylaşılan kaynak: yüklenmiş dosya veya YouTube videosu.
/// Projeye erişimi olan tüm üyeler görüntüler/indirir.
/// </summary>
public class ProjectResource : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public ResourceType Type { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>File için göreli yol (/uploads/..), YouTube için video URL'i.</summary>
    [MaxLength(512)]
    public string? Url { get; set; }

    /// <summary>File için orijinal dosya adı (indirme adı).</summary>
    [MaxLength(256)]
    public string? FileName { get; set; }

    [MaxLength(128)]
    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }
}
