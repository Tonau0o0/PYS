using PYS.Core.Common;

namespace PYS.Service.DTOs.Auth;

public sealed record AuthResponseDto(
    string AccessToken,
    DateTime ExpiresAt,
    int UserId,
    string UserName,
    string FullName,
    UserRole Role,
    string Color,
    string? AvatarUrl);
