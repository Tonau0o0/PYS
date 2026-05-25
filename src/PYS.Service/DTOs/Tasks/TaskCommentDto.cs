using System.ComponentModel.DataAnnotations;

namespace PYS.Service.DTOs.Tasks;

public sealed record TaskCommentDto(
    int Id,
    int TaskId,
    string Content,
    int AuthorId,
    string AuthorName,
    string? AuthorColor,
    string? AuthorAvatarUrl,
    DateTime CreatedAt);

public sealed class AddCommentDto
{
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}
