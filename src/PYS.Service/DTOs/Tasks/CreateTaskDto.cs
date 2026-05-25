using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Service.DTOs.Tasks;

public sealed class CreateTaskDto
{
    [Required, MinLength(2), MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    public int? AssigneeId { get; set; }
}
