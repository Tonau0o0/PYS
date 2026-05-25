using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Core.Entities;

public class ProjectTask : BaseEntity
{
    [Required, MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? AssigneeId { get; set; }
    public User? Assignee { get; set; }
}
