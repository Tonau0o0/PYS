using Microsoft.EntityFrameworkCore;
using PYS.Core.Abstractions;
using PYS.Core.Common;
using PYS.Core.Entities;
using PYS.Service.Common;
using PYS.Service.DTOs.Projects;
using PYS.Service.Interfaces;

namespace PYS.Service.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<ProjectMember> _memberRepository;
    private readonly IRepository<ProjectInvitation> _invitationRepository;
    private readonly ICurrentUserService _currentUser;

    public ProjectService(
        IRepository<Project> projectRepository,
        IRepository<User> userRepository,
        IRepository<ProjectMember> memberRepository,
        IRepository<ProjectInvitation> invitationRepository,
        ICurrentUserService currentUser)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _memberRepository = memberRepository;
        _invitationRepository = invitationRepository;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectDto>>> GetAllAsync(ProjectFilterDto? filter, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<IReadOnlyList<ProjectDto>>.Failure("Authentication required.");
        }

        IQueryable<Project> query = _projectRepository.Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .Where(p => p.OwnerId == uid || p.Members.Any(m => m.UserId == uid));

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (filter.Status.HasValue)
                query = query.Where(p => p.Status == filter.Status.Value);

            if (filter.StartDateFrom.HasValue)
                query = query.Where(p => p.StartDate >= filter.StartDateFrom.Value);

            if (filter.StartDateTo.HasValue)
                query = query.Where(p => p.StartDate <= filter.StartDateTo.Value);
        }

        var data = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectDto(
                p.Id, p.Name, p.Description, p.Status, p.StartDate, p.EndDate,
                p.OwnerId, p.Owner!.UserName,
                p.Tasks.Count, p.Members.Count,
                p.OwnerId == uid
                    ? ProjectRole.Owner
                    : p.Members.Where(m => m.UserId == uid).Select(m => m.Role).FirstOrDefault(),
                p.OwnerId == uid,
                p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectDto>>.Success(data);
    }

    public async Task<ServiceResult<ProjectDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<ProjectDto>.Failure("Authentication required.");
        }

        var project = await _projectRepository.Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project is null)
        {
            return ServiceResult<ProjectDto>.NotFound($"Project {id} not found.");
        }

        if (!HasAccess(project, uid))
        {
            return ServiceResult<ProjectDto>.NotFound($"Project {id} not found.");
        }

        return ServiceResult<ProjectDto>.Success(ToDto(project, uid));
    }

    public async Task<ServiceResult<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<ProjectDto>.Failure("Authentication required.");
        }

        var errors = Validate(dto.Name, dto.StartDate, dto.EndDate);
        if (errors.Count > 0)
        {
            return ServiceResult<ProjectDto>.ValidationFailed(errors);
        }

        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Status = dto.Status,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            OwnerId = uid
        };
        await _projectRepository.AddAsync(project, cancellationToken);

        var ownerMember = new ProjectMember
        {
            Project = project,
            UserId = uid,
            Role = ProjectRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        await _memberRepository.AddAsync(ownerMember, cancellationToken);

        await _projectRepository.SaveChangesAsync(cancellationToken);

        var created = await _projectRepository.Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .FirstAsync(p => p.Id == project.Id, cancellationToken);

        return ServiceResult<ProjectDto>.Success(ToDto(created, uid));
    }

    public async Task<ServiceResult<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<ProjectDto>.Failure("Authentication required.");
        }

        var errors = Validate(dto.Name, dto.StartDate, dto.EndDate);
        if (errors.Count > 0)
        {
            return ServiceResult<ProjectDto>.ValidationFailed(errors);
        }

        var entity = await _projectRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult<ProjectDto>.NotFound($"Project {id} not found.");
        }

        if (entity.OwnerId != uid)
        {
            return ServiceResult<ProjectDto>.Failure("Only the project owner can update this project.");
        }

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Status = dto.Status;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;

        _projectRepository.Update(entity);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        var updated = await _projectRepository.Query()
            .Include(p => p.Owner)
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .FirstAsync(p => p.Id == id, cancellationToken);

        return ServiceResult<ProjectDto>.Success(ToDto(updated, uid));
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult.Failure("Authentication required.");
        }

        var entity = await _projectRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is null)
        {
            return ServiceResult.NotFound($"Project {id} not found.");
        }

        if (entity.OwnerId != uid)
        {
            return ServiceResult.Failure("Only the project owner can delete this project.");
        }

        _projectRepository.Remove(entity);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectMemberDto>>> GetMembersAsync(int projectId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<IReadOnlyList<ProjectMemberDto>>.Failure("Authentication required.");
        }

        var hasAccess = await _projectRepository.Query()
            .AnyAsync(p => p.Id == projectId && (p.OwnerId == uid || p.Members.Any(m => m.UserId == uid)), cancellationToken);

        if (!hasAccess)
        {
            return ServiceResult<IReadOnlyList<ProjectMemberDto>>.NotFound($"Project {projectId} not found.");
        }

        var members = await _memberRepository.Query()
            .Include(m => m.User)
            .Where(m => m.ProjectId == projectId)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new ProjectMemberDto(
                m.UserId, m.User!.UserName, m.User.Email, m.User.FullName, m.Role, m.User.ColorHex, m.JoinedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectMemberDto>>.Success(members);
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectInvitationDto>>> GetInvitationsAsync(int projectId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult<IReadOnlyList<ProjectInvitationDto>>.Failure("Authentication required.");
        }

        var project = await _projectRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null || project.OwnerId != uid)
        {
            return ServiceResult<IReadOnlyList<ProjectInvitationDto>>.NotFound($"Project {projectId} not found.");
        }

        var data = await _invitationRepository.Query()
            .Where(i => i.ProjectId == projectId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ProjectInvitationDto(i.Id, i.Email, i.Status, i.CreatedAt, i.CreatedBy))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectInvitationDto>>.Success(data);
    }

    public async Task<ServiceResult> InviteAsync(int projectId, InviteMemberDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult.Failure("Authentication required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return ServiceResult.ValidationFailed(new[] { "Email is required." });
        }

        var project = await _projectRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return ServiceResult.NotFound($"Project {projectId} not found.");
        }
        if (project.OwnerId != uid)
        {
            return ServiceResult.Failure("Only the project owner can invite members.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.Query()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (existingUser is not null)
        {
            var alreadyMember = await _memberRepository.Query()
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == existingUser.Id, cancellationToken);

            if (alreadyMember)
            {
                return ServiceResult.Conflict($"{email} is already a member of this project.");
            }

            await _memberRepository.AddAsync(new ProjectMember
            {
                ProjectId = projectId,
                UserId = existingUser.Id,
                Role = ProjectRole.Member,
                JoinedAt = DateTime.UtcNow
            }, cancellationToken);

            await _memberRepository.SaveChangesAsync(cancellationToken);
            return ServiceResult.Success();
        }

        var existingInvitation = await _invitationRepository.Query()
            .FirstOrDefaultAsync(i => i.ProjectId == projectId && i.Email == email, cancellationToken);

        if (existingInvitation is not null)
        {
            return ServiceResult.Conflict($"An invitation to {email} already exists.");
        }

        await _invitationRepository.AddAsync(new ProjectInvitation
        {
            ProjectId = projectId,
            Email = email,
            Status = InvitationStatus.Pending
        }, cancellationToken);

        await _invitationRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveMemberAsync(int projectId, int userId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult.Failure("Authentication required.");
        }

        var project = await _projectRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
        {
            return ServiceResult.NotFound($"Project {projectId} not found.");
        }
        if (project.OwnerId != uid)
        {
            return ServiceResult.Failure("Only the project owner can remove members.");
        }
        if (userId == project.OwnerId)
        {
            return ServiceResult.Failure("Owner cannot be removed.");
        }

        var member = await _memberRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, cancellationToken);
        if (member is null)
        {
            return ServiceResult.NotFound("Member not found in this project.");
        }

        _memberRepository.Remove(member);
        await _memberRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CancelInvitationAsync(int projectId, int invitationId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
        {
            return ServiceResult.Failure("Authentication required.");
        }

        var project = await _projectRepository.Query()
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);
        if (project is null || project.OwnerId != uid)
        {
            return ServiceResult.NotFound($"Project {projectId} not found.");
        }

        var invitation = await _invitationRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.ProjectId == projectId, cancellationToken);
        if (invitation is null)
        {
            return ServiceResult.NotFound("Invitation not found.");
        }

        _invitationRepository.Remove(invitation);
        await _invitationRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static bool HasAccess(Project p, int userId)
        => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId);

    private static ProjectDto ToDto(Project p, int currentUserId)
    {
        var role = p.OwnerId == currentUserId
            ? ProjectRole.Owner
            : p.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).FirstOrDefault();

        return new ProjectDto(
            p.Id, p.Name, p.Description, p.Status, p.StartDate, p.EndDate,
            p.OwnerId, p.Owner?.UserName,
            p.Tasks?.Count ?? 0, p.Members?.Count ?? 0,
            role, p.OwnerId == currentUserId,
            p.CreatedAt, p.CreatedBy, p.UpdatedAt, p.UpdatedBy);
    }

    private static List<string> Validate(string name, DateTime startDate, DateTime? endDate)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add("Project name is required.");
        if (endDate.HasValue && endDate.Value.Date < startDate.Date) errors.Add("EndDate cannot be earlier than StartDate.");
        return errors;
    }
}
