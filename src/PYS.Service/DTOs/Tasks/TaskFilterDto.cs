using PYS.Core.Common;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Service.DTOs.Tasks;

public sealed class TaskFilterDto
{
    public string? Search { get; set; }
    public int? ProjectId { get; set; }
    public int? AssigneeId { get; set; }
    public TaskStatusEnum? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public DateTime? DueDateBefore { get; set; }
    public DateTime? DueDateAfter { get; set; }
}
