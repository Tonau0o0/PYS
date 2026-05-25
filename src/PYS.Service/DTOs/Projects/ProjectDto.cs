using PYS.Core.Common;

namespace PYS.Service.DTOs.Projects;

public sealed record ProjectDto(
    int Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime StartDate,
    DateTime? EndDate,
    int OwnerId,
    string? OwnerUserName,
    int TaskCount,
    int MemberCount,
    ProjectRole CurrentUserRole,
    bool IsOwner,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);
