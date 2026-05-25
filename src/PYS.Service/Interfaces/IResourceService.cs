using PYS.Service.Common;
using PYS.Service.DTOs.Resources;

namespace PYS.Service.Interfaces;

public interface IResourceService
{
    /// <summary>Bir klasörün (null = kök) doğrudan içindeki kaynakları döner.</summary>
    Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetForProjectAsync(int projectId, int? parentFolderId, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectResourceDto>> CreateFolderAsync(int projectId, CreateFolderDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectResourceDto>> AddFileAsync(
        int projectId, string title, int? parentFolderId, Stream content, string fileName, string? contentType, long sizeBytes, CancellationToken cancellationToken = default);

    Task<ServiceResult<ProjectResourceDto>> AddYouTubeAsync(int projectId, AddYouTubeDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> MoveAsync(int projectId, int resourceId, int? newParentFolderId, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(int projectId, int resourceId, CancellationToken cancellationToken = default);

    /// <summary>Dosyayı olduğu gibi, klasörü ise içeriğini zip olarak indirilebilir akış döner.</summary>
    Task<ServiceResult<PYS.Core.Abstractions.FileContent>> GetDownloadAsync(int projectId, int resourceId, CancellationToken cancellationToken = default);

    // Görev ↔ kaynak bağları
    Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetTaskResourcesAsync(int taskId, CancellationToken cancellationToken = default);
    Task<ServiceResult> LinkToTaskAsync(int taskId, int resourceId, CancellationToken cancellationToken = default);
    Task<ServiceResult> UnlinkFromTaskAsync(int taskId, int resourceId, CancellationToken cancellationToken = default);
}
