using PYS.Core.Common;

namespace PYS.Service.DTOs.Projects;

public sealed record ProjectMemberDto(
    int UserId,
    string UserName,
    string Email,
    string FullName,
    ProjectRole Role,
    string Color,
    string? AvatarUrl,
    DateTime JoinedAt);

public sealed record ProjectInvitationDto(
    int Id,
    string Email,
    InvitationStatus Status,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed class InviteMemberDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    [System.ComponentModel.DataAnnotations.MaxLength(128)]
    public string Email { get; set; } = string.Empty;
}
