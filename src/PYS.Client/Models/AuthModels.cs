using PYS.Core.Common;

namespace PYS.Client.Models;

public sealed record LoginRequest(string UserName, string Password);

public sealed record RegisterRequest(string UserName, string Email, string FullName, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    int UserId,
    string UserName,
    string FullName,
    UserRole Role,
    string Color);

public sealed record UpdateColorRequest(string Color);
