using Microsoft.EntityFrameworkCore;
using PYS.Core.Abstractions;
using PYS.Core.Entities;
using PYS.Service.Common;
using PYS.Service.DTOs.Tasks;
using PYS.Service.Interfaces;
using TaskStatusEnum = PYS.Core.Common.TaskStatus;

namespace PYS.Service.Services;

public sealed class TaskService : ITaskService
{
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectMember> _memberRepository;
    private readonly ICurrentUserService _currentUser;

    public TaskService(
        IRepository<ProjectTask> taskRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectMember> memberRepository,
        ICurrentUserService currentUser)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _memberRepository = memberRepository;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<IReadOnlyList<TaskDto>>> GetAllAsync(TaskFilterDto? filter, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<IReadOnlyList<TaskDto>>.Failure("Authentication required.");
        }

        IQueryable<ProjectTask> query = _taskRepository.Query()
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .Where(t => t.Project!.OwnerId == uid || t.Project.Members.Any(m => m.UserId == uid));

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)));
            }

            if (filter.ProjectId.HasValue) query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
            if (filter.AssigneeId.HasValue) query = query.Where(t => t.AssigneeId == filter.AssigneeId.Value);
            if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
            if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority.Value);
            if (filter.DueDateAfter.HasValue) query = query.Where(t => t.DueDate >= filter.DueDateAfter.Value);
            if (filter.DueDateBefore.HasValue) query = query.Where(t => t.DueDate <= filter.DueDateBefore.Value);
        }

        var data = await query
            .OrderBy(t => t.Status)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Select(t => new TaskDto(
                t.Id, t.Title, t.Description, t.Status, t.Priority,
                t.DueDate, t.CompletedAt,
                t.ProjectId, t.Project!.Name,
                t.AssigneeId, t.Assignee != null ? t.Assignee.UserName : null,
                t.Assignee != null ? t.Assignee.ColorHex : null,
                t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<TaskDto>>.Success(data);
    }

    public async Task<ServiceResult<TaskDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<TaskDto>.Failure("Authentication required.");
        }

        var entity = await _taskRepository.Query()
            .Include(t => t.Project)
            .ThenInclude(p => p!.Members)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult<TaskDto>.NotFound($"Task {id} not found.");
        }

        if (!HasAccess(entity, uid))
        {
            return ServiceResult<TaskDto>.NotFound($"Task {id} not found.");
        }

        return ServiceResult<TaskDto>.Success(entity.ToDto());
    }

    public async Task<ServiceResult<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<TaskDto>.Failure("Authentication required.");
        }

        var project = await _projectRepository.Query()
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId, cancellationToken);

        if (project is null || (project.OwnerId != uid && !project.Members.Any(m => m.UserId == uid)))
        {
            return ServiceResult<TaskDto>.NotFound($"Project {dto.ProjectId} not found.");
        }

        if (dto.AssigneeId.HasValue)
        {
            var assigneeIsMember = project.OwnerId == dto.AssigneeId.Value ||
                                   project.Members.Any(m => m.UserId == dto.AssigneeId.Value);
            if (!assigneeIsMember)
            {
                return ServiceResult<TaskDto>.ValidationFailed(new[] { "Assignee must be a member of this project." });
            }
        }

        var entity = new ProjectTask
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Status = dto.Status,
            Priority = dto.Priority,
            DueDate = dto.DueDate,
            ProjectId = dto.ProjectId,
            AssigneeId = dto.AssigneeId,
            CompletedAt = dto.Status == TaskStatusEnum.Done ? DateTime.UtcNow : null
        };

        await _taskRepository.AddAsync(entity, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var created = await _taskRepository.Query()
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .FirstAsync(t => t.Id == entity.Id, cancellationToken);

        return ServiceResult<TaskDto>.Success(created.ToDto());
    }

    public async Task<ServiceResult<TaskDto>> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<TaskDto>.Failure("Authentication required.");
        }

        var entity = await _taskRepository.Query(asNoTracking: false)
            .Include(t => t.Project)
            .ThenInclude(p => p!.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult<TaskDto>.NotFound($"Task {id} not found.");
        }

        if (!HasAccess(entity, uid))
        {
            return ServiceResult<TaskDto>.NotFound($"Task {id} not found.");
        }

        if (dto.AssigneeId.HasValue)
        {
            var assigneeIsMember = entity.Project!.OwnerId == dto.AssigneeId.Value ||
                                   entity.Project.Members.Any(m => m.UserId == dto.AssigneeId.Value);
            if (!assigneeIsMember)
            {
                return ServiceResult<TaskDto>.ValidationFailed(new[] { "Assignee must be a member of this project." });
            }
        }

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Priority = dto.Priority;
        entity.DueDate = dto.DueDate;
        entity.AssigneeId = dto.AssigneeId;

        if (entity.Status != dto.Status)
        {
            entity.Status = dto.Status;
            entity.CompletedAt = dto.Status == TaskStatusEnum.Done ? DateTime.UtcNow : null;
        }

        _taskRepository.Update(entity);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var updated = await _taskRepository.Query()
            .Include(t => t.Project)
            .Include(t => t.Assignee)
            .FirstAsync(t => t.Id == id, cancellationToken);

        return ServiceResult<TaskDto>.Success(updated.ToDto());
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult.Failure("Authentication required.");
        }

        var entity = await _taskRepository.Query(asNoTracking: false)
            .Include(t => t.Project)
            .ThenInclude(p => p!.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult.NotFound($"Task {id} not found.");
        }

        if (!HasAccess(entity, uid))
        {
            return ServiceResult.NotFound($"Task {id} not found.");
        }

        _taskRepository.Remove(entity);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private static bool HasAccess(ProjectTask t, int uid)
        => t.Project is not null &&
           (t.Project.OwnerId == uid || t.Project.Members.Any(m => m.UserId == uid));
}
