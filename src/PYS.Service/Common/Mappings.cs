using PYS.Core.Entities;
using PYS.Service.DTOs.Projects;
using PYS.Service.DTOs.Tasks;

namespace PYS.Service.Common;

internal static class Mappings
{
    public static ProjectDto ToDto(this Project p, int currentUserId)
    {
        var role = p.OwnerId == currentUserId
            ? PYS.Core.Common.ProjectRole.Owner
            : p.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).FirstOrDefault();

        return new ProjectDto(
            p.Id, p.Name, p.Description, p.Status, p.StartDate, p.EndDate,
            p.OwnerId, p.Owner?.UserName,
            p.Tasks?.Count ?? 0, p.Members?.Count ?? 0,
            role, p.OwnerId == currentUserId,
            p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy);
    }

    public static TaskDto ToDto(this ProjectTask t) => new(
        t.Id, t.Title, t.Description, t.Status, t.Priority,
        t.DueDate, t.CompletedAt,
        t.ProjectId, t.Project?.Name,
        t.AssigneeId, t.Assignee?.UserName, t.Assignee?.ColorHex, t.Assignee?.AvatarUrl,
        t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy);
}
