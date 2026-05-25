using Microsoft.AspNetCore.Mvc;
using PYS.API.Common;
using PYS.Service.DTOs.Resources;
using PYS.Service.Interfaces;

namespace PYS.API.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/projects/{projectId:int}/resources")
            .WithTags("Resources")
            .RequireAuthorization();

        group.MapGet("", async (int projectId, IResourceService service, CancellationToken ct) =>
            (await service.GetForProjectAsync(projectId, ct)).ToHttp());

        group.MapPost("/file", async (
            int projectId,
            IFormFile file,
            [FromForm] string? title,
            IResourceService service,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0) return Results.BadRequest(new { error = "No file uploaded." });
            if (file.Length > 25 * 1024 * 1024) return Results.BadRequest(new { error = "File exceeds 25 MB limit." });

            await using var stream = file.OpenReadStream();
            return (await service.AddFileAsync(projectId, title ?? file.FileName, stream, file.FileName, file.ContentType, file.Length, ct)).ToHttp();
        })
        .DisableAntiforgery();

        group.MapPost("/link", async (
            int projectId,
            AddYouTubeDto dto,
            IResourceService service,
            CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.AddYouTubeAsync(projectId, dto, ct)).ToHttp();
        });

        group.MapDelete("/{resourceId:int}", async (
            int projectId, int resourceId, IResourceService service, CancellationToken ct) =>
            (await service.DeleteAsync(projectId, resourceId, ct)).ToHttp());

        return routes;
    }
}
