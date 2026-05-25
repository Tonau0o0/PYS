using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Core.Entities;

public class User : BaseEntity
{
    [Required, MaxLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(128), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Member;

    public bool IsActive { get; set; } = true;

    [Required, MaxLength(9)]
    public string ColorHex { get; set; } = "#2196F3";

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
