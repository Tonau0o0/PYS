using System.ComponentModel.DataAnnotations;

namespace PYS.Service.DTOs.Auth;

public sealed class RegisterDto
{
    [Required, MinLength(3), MaxLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(128)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(2), MaxLength(64)]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(64)]
    public string Password { get; set; } = string.Empty;
}
