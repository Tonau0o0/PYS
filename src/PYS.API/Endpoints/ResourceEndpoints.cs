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

        // Belirli bir klasörün (folderId yoksa kök) içeriği
        group.MapGet("", async (int projectId, int? folderId, IResourceService service, CancellationToken ct) =>
            (await service.GetForProjectAsync(projectId, folderId, ct)).ToHttp());

        group.MapPost("/folder", async (int projectId, CreateFolderDto dto, IResourceService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.CreateFolderAsync(projectId, dto, ct)).ToHttp();
        });

        group.MapPost("/file", async (
            int projectId,
            IFormFile file,
            [FromForm] string? title,
            [FromForm] int? parentFolderId,
            IResourceService service,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0) return Results.BadRequest(new { error = "No file uploaded." });
            if (file.Length > 25 * 1024 * 1024) return Results.BadRequest(new { error = "File exceeds 25 MB limit." });

            await using var stream = file.OpenReadStream();
            return (await service.AddFileAsync(projectId, title ?? file.FileName, parentFolderId, stream, file.FileName, file.ContentType, file.Length, ct)).ToHttp();
        })
        .DisableAntiforgery();

        group.MapPost("/link", async (int projectId, AddYouTubeDto dto, IResourceService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.AddYouTubeAsync(projectId, dto, ct)).ToHttp();
        });

        group.MapPut("/{resourceId:int}/move", async (
            int projectId, int resourceId, MoveResourceDto dto, IResourceService service, CancellationToken ct) =>
            (await service.MoveAsync(projectId, resourceId, dto.ParentFolderId, ct)).ToHttp());

        group.MapDelete("/{resourceId:int}", async (
            int projectId, int resourceId, IResourceService service, CancellationToken ct) =>
            (await service.DeleteAsync(projectId, resourceId, ct)).ToHttp());

        // Görev ↔ kaynak bağları
        var taskGroup = routes.MapGroup("/api/tasks/{taskId:int}/resources")
            .WithTags("Resources")
            .RequireAuthorization();

        taskGroup.MapGet("", async (int taskId, IResourceService service, CancellationToken ct) =>
            (await service.GetTaskResourcesAsync(taskId, ct)).ToHttp());

        taskGroup.MapPost("/{resourceId:int}", async (int taskId, int resourceId, IResourceService service, CancellationToken ct) =>
            (await service.LinkToTaskAsync(taskId, resourceId, ct)).ToHttp());

        taskGroup.MapDelete("/{resourceId:int}", async (int taskId, int resourceId, IResourceService service, CancellationToken ct) =>
            (await service.UnlinkFromTaskAsync(taskId, resourceId, ct)).ToHttp());

        return routes;
    }
}
