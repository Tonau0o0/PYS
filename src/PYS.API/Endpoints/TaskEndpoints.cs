using PYS.API.Common;
using PYS.Core.Common;
using PYS.Service.DTOs.Tasks;
using PYS.Service.Interfaces;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.API.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks").WithTags("Tasks").RequireAuthorization();

        group.MapGet("/", async (
            ITaskService service,
            string? search,
            int? projectId,
            int? assigneeId,
            int? status,
            int? priority,
            DateTime? dueDateBefore,
            DateTime? dueDateAfter,
            CancellationToken ct) =>
        {
            var filter = new TaskFilterDto
            {
                Search = search,
                ProjectId = projectId,
                AssigneeId = assigneeId,
                Status = status.HasValue ? (TaskStatusEnum)status.Value : null,
                Priority = priority.HasValue ? (TaskPriority)priority.Value : null,
                DueDateBefore = dueDateBefore,
                DueDateAfter = dueDateAfter
            };
            return (await service.GetAllAsync(filter, ct)).ToHttp();
        });

        group.MapGet("/{id:int}", async (int id, ITaskService service, CancellationToken ct) =>
            (await service.GetByIdAsync(id, ct)).ToHttp());

        group.MapPost("/", async (CreateTaskDto dto, ITaskService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.CreateAsync(dto, ct)).ToCreated("/api/tasks");
        });

        group.MapPut("/{id:int}", async (int id, UpdateTaskDto dto, ITaskService service, CancellationToken ct) =>
        {
            var validation = DataAnnotationsValidator.Validate(dto);
            if (validation is not null) return validation;
            return (await service.UpdateAsync(id, dto, ct)).ToHttp();
        });

        group.MapDelete("/{id:int}", async (int id, ITaskService service, CancellationToken ct) =>
            (await service.DeleteAsync(id, ct)).ToHttp());

        return routes;
    }
}
