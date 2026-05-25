using PYS.Service.Common;
using PYS.Service.DTOs.Projects;

namespace PYS.Service.Interfaces;

public interface IProjectService
{
    Task<ServiceResult<IReadOnlyList<ProjectDto>>> GetAllAsync(ProjectFilterDto? filter, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProjectDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProjectDto>> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<ProjectDto>> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<ProjectMemberDto>>> GetMembersAsync(int projectId, CancellationToken cancellationToken = default);
    Task<ServiceResult<IReadOnlyList<ProjectInvitationDto>>> GetInvitationsAsync(int projectId, CancellationToken cancellationToken = default);
    Task<ServiceResult> InviteAsync(int projectId, InviteMemberDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveMemberAsync(int projectId, int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelInvitationAsync(int projectId, int invitationId, CancellationToken cancellationToken = default);
}
