using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Core.Entities;

public class ProjectInvitation : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Required, MaxLength(128), EmailAddress]
    public string Email { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime? AcceptedAt { get; set; }
}
