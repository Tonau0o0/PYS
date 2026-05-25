using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Service.DTOs.Projects;

public sealed class UpdateProjectDto
{
    [Required, MinLength(2), MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
