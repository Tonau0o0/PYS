using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Service.DTOs.Resources;

public sealed record ProjectResourceDto(
    int Id,
    int ProjectId,
    ResourceType Type,
    string Title,
    string? Url,
    string? FileName,
    string? ContentType,
    long? SizeBytes,
    string? YouTubeId,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed class AddYouTubeDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(512), Url]
    public string Url { get; set; } = string.Empty;
}
