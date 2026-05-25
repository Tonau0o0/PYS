using System.ComponentModel.DataAnnotations;

namespace PYS.Service.DTOs.Auth;

public sealed class ChangePasswordDto
{
    [Required, MinLength(6), MaxLength(64)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(64)]
    public string NewPassword { get; set; } = string.Empty;
}
