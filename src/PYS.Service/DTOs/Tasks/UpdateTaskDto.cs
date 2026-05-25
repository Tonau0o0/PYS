using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Service.DTOs.Tasks;

public sealed class UpdateTaskDto
{
    [Required, MinLength(2), MaxLength(128)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTime? DueDate { get; set; }

    public int? AssigneeId { get; set; }
}
