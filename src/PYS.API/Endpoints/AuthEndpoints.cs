using System.Security.Claims;
using PYS.API.Common;
using PYS.Service.DTOs.Auth;
using PYS.Service.Interfaces;

namespace PYS.API.Endpoints;

public sealed record UpdateColorRequest(string Color);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterDto dto, IAuthService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.RegisterAsync(dto, ct)).ToHttp();
        })
        .AllowAnonymous();

        group.MapPost("/login", async (LoginDto dto, IAuthService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            var result = await service.LoginAsync(dto, ct);
            return result.IsSuccess
                ? Results.Ok(result.Data)
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        })
        .AllowAnonymous();

        group.MapPost("/logout", (ClaimsPrincipal user) =>
        {
            return Results.Ok(new { message = "Logged out. Discard the access token client-side." });
        })
        .RequireAuthorization();

        group.MapPost("/change-password", async (
            ChangePasswordDto dto,
            ClaimsPrincipal user,
            IAuthService service,
            CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId))
            {
                return Results.Unauthorized();
            }
            return (await service.ChangePasswordAsync(userId, dto, ct)).ToHttp();
        })
        .RequireAuthorization();

        group.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            userName = user.Identity?.Name,
            email = user.FindFirst(ClaimTypes.Email)?.Value,
            role = user.FindFirst(ClaimTypes.Role)?.Value
        }))
        .RequireAuthorization();

        group.MapPut("/me/color", async (
            UpdateColorRequest req,
            ClaimsPrincipal user,
            IAuthService service,
            CancellationToken ct) =>
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userId)) return Results.Unauthorized();
            return (await service.UpdateColorAsync(userId, req.Color, ct)).ToHttp();
        })
        .RequireAuthorization();

        return routes;
    }
}
