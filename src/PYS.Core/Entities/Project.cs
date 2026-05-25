using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Core.Entities;

public class Project : BaseEntity
{
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public ICollection<ProjectInvitation> Invitations { get; set; } = new List<ProjectInvitation>();
}
