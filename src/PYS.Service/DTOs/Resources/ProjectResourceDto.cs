using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Service.DTOs.Resources;

public sealed record ProjectResourceDto(
    int Id,
    int ProjectId,
    int? ParentFolderId,
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

    public int? ParentFolderId { get; set; }
}

public sealed class CreateFolderDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? ParentFolderId { get; set; }
}

public sealed class MoveResourceDto
{
    public int? ParentFolderId { get; set; }
}
