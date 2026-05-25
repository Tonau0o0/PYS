using System.ComponentModel.DataAnnotations;

namespace PYS.Service.DTOs.Auth;

public sealed class UpdateProfileDto
{
    [Required, MinLength(2), MaxLength(64)]
    public string FullName { get; set; } = string.Empty;
}

public sealed class UpdateEmailDto
{
    [Required, EmailAddress, MaxLength(128)]
    public string Email { get; set; } = string.Empty;
}
