using PYS.Core.Common;

namespace PYS.Core.Entities;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public ProjectRole Role { get; set; } = ProjectRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
