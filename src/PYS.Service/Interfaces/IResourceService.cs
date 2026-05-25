using PYS.Service.Common;
using PYS.Service.DTOs.Resources;

namespace PYS.Service.Interfaces;

public interface IResourceService
{
    Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetForProjectAsync(int projectId, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectResourceDto>> AddFileAsync(
        int projectId, string title, Stream content, string fileName, string? contentType, long sizeBytes, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectResourceDto>> AddYouTubeAsync(int projectId, AddYouTubeDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(int projectId, int resourceId, CancellationToken cancellationToken = default);
}
