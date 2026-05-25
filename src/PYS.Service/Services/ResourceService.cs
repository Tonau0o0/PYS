using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PYS.Core.Abstractions;
using PYS.Core.Common;
using PYS.Core.Entities;
using PYS.Service.Common;
using PYS.Service.DTOs.Resources;
using PYS.Service.Interfaces;

namespace PYS.Service.Services;

public sealed class ResourceService : IResourceService
{
    private readonly IRepository<ProjectResource> _resourceRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _fileStorage;

    private static readonly Regex YouTubeIdRegex = new(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ResourceService(
        IRepository<ProjectResource> resourceRepository,
        IRepository<Project> projectRepository,
        ICurrentUserService currentUser,
        IFileStorage fileStorage)
    {
        _resourceRepository = resourceRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetForProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.NotFound($"Project {projectId} not found.");

        var data = await _resourceRepository.Query()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Success(data.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ProjectResourceDto>> AddFileAsync(
        int projectId, string title, Stream content, string fileName, string? contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<ProjectResourceDto>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<ProjectResourceDto>.NotFound($"Project {projectId} not found.");

        var relativeUrl = await _fileStorage.SaveAsync("uploads", fileName, content, cancellationToken);

        var entity = new ProjectResource
        {
            ProjectId = projectId,
            Type = ResourceType.File,
            Title = string.IsNullOrWhiteSpace(title) ? fileName : title.Trim(),
            Url = relativeUrl,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes
        };

        await _resourceRepository.AddAsync(entity, cancellationToken);
        await _resourceRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProjectResourceDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult<ProjectResourceDto>> AddYouTubeAsync(int projectId, AddYouTubeDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<ProjectResourceDto>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<ProjectResourceDto>.NotFound($"Project {projectId} not found.");

        var videoId = ExtractYouTubeId(dto.Url);
        if (videoId is null)
            return ServiceResult<ProjectResourceDto>.ValidationFailed(new[] { "Geçerli bir YouTube linki değil." });

        var entity = new ProjectResource
        {
            ProjectId = projectId,
            Type = ResourceType.YouTube,
            Title = dto.Title.Trim(),
            Url = dto.Url.Trim()
        };

        await _resourceRepository.AddAsync(entity, cancellationToken);
        await _resourceRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<ProjectResourceDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult> DeleteAsync(int projectId, int resourceId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult.Failure("Authentication required.");

        var project = await _projectRepository.Query()
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null || (project.OwnerId != uid && !project.Members.Any(m => m.UserId == uid)))
            return ServiceResult.NotFound($"Project {projectId} not found.");

        var entity = await _resourceRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.ProjectId == projectId, cancellationToken);

        if (entity is null)
            return ServiceResult.NotFound($"Resource {resourceId} not found.");

        // Yalnız proje sahibi veya kaynağı ekleyen kişi silebilir.
        var isOwner = project.OwnerId == uid;
        var isCreator = string.Equals(entity.CreatedBy, _currentUser.UserName, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !isCreator)
            return ServiceResult.Failure("Bu kaynağı yalnız ekleyen kişi veya proje sahibi silebilir.");

        if (entity.Type == ResourceType.File)
            await _fileStorage.DeleteAsync(entity.Url, cancellationToken);

        _resourceRepository.Remove(entity);
        await _resourceRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private async Task<bool> HasAccessAsync(int projectId, int uid, CancellationToken cancellationToken)
        => await _projectRepository.Query()
            .AnyAsync(p => p.Id == projectId && (p.OwnerId == uid || p.Members.Any(m => m.UserId == uid)), cancellationToken);

    private static ProjectResourceDto ToDto(ProjectResource r) => new(
        r.Id, r.ProjectId, r.Type, r.Title, r.Url, r.FileName, r.ContentType, r.SizeBytes,
        r.Type == ResourceType.YouTube ? ExtractYouTubeId(r.Url) : null,
        r.CreatedAt, r.CreatedBy);

    private static string? ExtractYouTubeId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = YouTubeIdRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }
}
