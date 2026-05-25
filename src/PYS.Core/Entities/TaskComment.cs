using System.ComponentModel.DataAnnotations;
using PYS.Core.Common;

namespace PYS.Core.Entities;

/// <summary>Bir göreve yapılan yorum. Yazar ve zaman BaseEntity audit + AuthorId ile tutulur.</summary>
public class TaskComment : BaseEntity
{
    public int TaskId { get; set; }
    public ProjectTask? Task { get; set; }

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
