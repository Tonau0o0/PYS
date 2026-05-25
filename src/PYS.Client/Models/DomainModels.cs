using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Client.Models;

public sealed record ProjectItem(
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

public sealed record ProjectMemberItem(
    int UserId,
    string UserName,
    string Email,
    string FullName,
    ProjectRole Role,
    string Color,
    string? AvatarUrl,
    DateTime JoinedAt);

public sealed record InvitationItem(
    int Id,
    string Email,
    InvitationStatus Status,
    DateTime CreatedAt,
    string? CreatedBy);

public sealed record TaskItem(
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

public sealed record ProjectResourceItem(
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
    string? CreatedBy)
{
    public bool IsYouTube => Type == ResourceType.YouTube;
    public bool IsFile => Type == ResourceType.File;

    public string? ThumbnailUrl => IsYouTube && !string.IsNullOrEmpty(YouTubeId)
        ? $"https://img.youtube.com/vi/{YouTubeId}/mqdefault.jpg"
        : null;

    public string MetaText => IsYouTube
        ? "▶ YouTube"
        : $"📄 {FormatSize(SizeBytes)}";

    private static string FormatSize(long? bytes)
    {
        if (bytes is not long b) return "Dosya";
        if (b < 1024) return $"{b} B";
        if (b < 1024 * 1024) return $"{b / 1024.0:0.#} KB";
        return $"{b / (1024.0 * 1024.0):0.#} MB";
    }
}

public sealed record AddYouTubeRequest(string Title, string Url);

public sealed record CreateProjectRequest(string Name, string? Description, ProjectStatus Status, DateTime StartDate, DateTime? EndDate);
public sealed record UpdateProjectRequest(string Name, string? Description, ProjectStatus Status, DateTime StartDate, DateTime? EndDate);

public sealed record InviteRequest(string Email);

public sealed record CreateTaskRequest(string Title, string? Description, TaskStatusEnum Status, TaskPriority Priority, DateTime? DueDate, int ProjectId, int? AssigneeId);
public sealed record UpdateTaskRequest(string Title, string? Description, TaskStatusEnum Status, TaskPriority Priority, DateTime? DueDate, int? AssigneeId);

public sealed record ApiError(string? Error, string[]? Details);
