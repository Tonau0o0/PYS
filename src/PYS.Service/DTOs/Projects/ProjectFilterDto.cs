using PYS.Core.Common;

namespace PYS.Service.DTOs.Projects;

public sealed class ProjectFilterDto
{
    public string? Search { get; set; }
    public ProjectStatus? Status { get; set; }
    public int? OwnerId { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
}
