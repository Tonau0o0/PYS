using System.ComponentModel.DataAnnotations;

namespace PYS.Core.Common;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(64)]
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
