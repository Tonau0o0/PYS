using PYS.API.Common;
using PYS.Core.Common;
using PYS.Service.DTOs.Projects;
using PYS.Service.Interfaces;

namespace PYS.API.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization();

        group.MapGet("/", async (
            IProjectService service,
            string? search,
            int? status,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            CancellationToken ct) =>
        {
            var filter = new ProjectFilterDto
            {
                Search = search,
                Status = status.HasValue ? (ProjectStatus)status.Value : null,
                StartDateFrom = startDateFrom,
                StartDateTo = startDateTo
            };
            return (await service.GetAllAsync(filter, ct)).ToHttp();
        });

        group.MapGet("/{id:int}", async (int id, IProjectService service, CancellationToken ct) =>
            (await service.GetByIdAsync(id, ct)).ToHttp());

        group.MapPost("/", async (CreateProjectDto dto, IProjectService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.CreateAsync(dto, ct)).ToCreated("/api/projects");
        });

        group.MapPut("/{id:int}", async (int id, UpdateProjectDto dto, IProjectService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.UpdateAsync(id, dto, ct)).ToHttp();
        });

        group.MapDelete("/{id:int}", async (int id, IProjectService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToHttp());

        group.MapGet("/{id:int}/members", async (int id, IProjectService service, CancellationToken ct) =>
            (await service.GetMembersAsync(id, ct)).ToHttp());

        group.MapGet("/{id:int}/invitations", async (int id, IProjectService service, CancellationToken ct) =>
            (await service.GetInvitationsAsync(id, ct)).ToHttp());

        group.MapPost("/{id:int}/invitations", async (int id, InviteMemberDto dto, IProjectService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.InviteAsync(id, dto, ct)).ToHttp();
        });

        group.MapDelete("/{id:int}/members/{userId:int}", async (int id, int userId, IProjectService service, CancellationToken ct) =>
            (await service.RemoveMemberAsync(id, userId, ct)).ToHttp());

        group.MapDelete("/{id:int}/invitations/{invitationId:int}", async (int id, int invitationId, IProjectService service, CancellationToken ct) =>
            (await service.CancelInvitationAsync(id, invitationId, ct)).ToHttp());

        return routes;
    }
}
