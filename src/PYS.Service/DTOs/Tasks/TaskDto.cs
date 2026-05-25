using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Service.DTOs.Tasks;

public sealed record TaskDto(
    int Id,
    string Title,
    string? Description,
    TaskStatusEnum Status,
    TaskPriority Priority,
    DateTime? DueDate,
    DateTime? CompletedAt,
    int ProjectId,
    string? ProjectName,
    int? AssigneeId,
    string? AssigneeUserName,
    string? AssigneeColor,
    string? AssigneeAvatarUrl,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);
