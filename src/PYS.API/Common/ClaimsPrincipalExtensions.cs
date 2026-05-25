using System.Security.Claims;

namespace PYS.API.Common;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out int userId)
        => int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}
