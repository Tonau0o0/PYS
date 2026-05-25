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
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly IRepository<TaskResource> _taskResourceRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _fileStorage;

    private static readonly Regex YouTubeIdRegex = new(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/|live/)|youtu\.be/)([A-Za-z0-9_-]{11})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ResourceService(
        IRepository<ProjectResource> resourceRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectTask> taskRepository,
        IRepository<TaskResource> taskResourceRepository,
        ICurrentUserService currentUser,
        IFileStorage fileStorage)
    {
        _resourceRepository = resourceRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _taskResourceRepository = taskResourceRepository;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetForProjectAsync(int projectId, int? parentFolderId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.NotFound($"Project {projectId} not found.");

        var data = await _resourceRepository.Query()
            .Where(r => r.ProjectId == projectId && r.ParentFolderId == parentFolderId)
            .OrderByDescending(r => r.Type == ResourceType.Folder)   // klasörler önce
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Success(data.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<ProjectResourceDto>> CreateFolderAsync(int projectId, CreateFolderDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<ProjectResourceDto>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<ProjectResourceDto>.NotFound($"Project {projectId} not found.");

        if (!await IsValidParentAsync(projectId, dto.ParentFolderId, cancellationToken))
            return ServiceResult<ProjectResourceDto>.ValidationFailed(new[] { "Geçersiz üst klasör." });

        var entity = new ProjectResource
        {
            ProjectId = projectId,
            ParentFolderId = dto.ParentFolderId,
            Type = ResourceType.Folder,
            Title = dto.Name.Trim()
        };

        await _resourceRepository.AddAsync(entity, cancellationToken);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProjectResourceDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult<ProjectResourceDto>> AddFileAsync(
        int projectId, string title, int? parentFolderId, Stream content, string fileName, string? contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<ProjectResourceDto>.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult<ProjectResourceDto>.NotFound($"Project {projectId} not found.");

        if (!await IsValidParentAsync(projectId, parentFolderId, cancellationToken))
            return ServiceResult<ProjectResourceDto>.ValidationFailed(new[] { "Geçersiz üst klasör." });

        var relativeUrl = await _fileStorage.SaveAsync("uploads", fileName, content, cancellationToken);

        var entity = new ProjectResource
        {
            ProjectId = projectId,
            ParentFolderId = parentFolderId,
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

        if (!await IsValidParentAsync(projectId, dto.ParentFolderId, cancellationToken))
            return ServiceResult<ProjectResourceDto>.ValidationFailed(new[] { "Geçersiz üst klasör." });

        var videoId = ExtractYouTubeId(dto.Url);
        if (videoId is null)
            return ServiceResult<ProjectResourceDto>.ValidationFailed(new[] { "Geçerli bir YouTube linki değil." });

        var entity = new ProjectResource
        {
            ProjectId = projectId,
            ParentFolderId = dto.ParentFolderId,
            Type = ResourceType.YouTube,
            Title = dto.Title.Trim(),
            Url = dto.Url.Trim()
        };

        await _resourceRepository.AddAsync(entity, cancellationToken);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProjectResourceDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult> MoveAsync(int projectId, int resourceId, int? newParentFolderId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult.Failure("Authentication required.");

        if (!await HasAccessAsync(projectId, uid, cancellationToken))
            return ServiceResult.NotFound($"Project {projectId} not found.");

        var entity = await _resourceRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.ProjectId == projectId, cancellationToken);
        if (entity is null)
            return ServiceResult.NotFound($"Resource {resourceId} not found.");

        if (newParentFolderId == resourceId)
            return ServiceResult.ValidationFailed(new[] { "Bir klasör kendi içine taşınamaz." });

        if (!await IsValidParentAsync(projectId, newParentFolderId, cancellationToken))
            return ServiceResult.ValidationFailed(new[] { "Geçersiz hedef klasör." });

        // Döngü koruması: hedef, taşınan klasörün alt klasörü olamaz.
        if (entity.Type == ResourceType.Folder && await IsDescendantAsync(projectId, resourceId, newParentFolderId, cancellationToken))
            return ServiceResult.ValidationFailed(new[] { "Klasör kendi alt klasörüne taşınamaz." });

        entity.ParentFolderId = newParentFolderId;
        _resourceRepository.Update(entity);
        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
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

        var isOwner = project.OwnerId == uid;
        var isCreator = string.Equals(entity.CreatedBy, _currentUser.UserName, StringComparison.OrdinalIgnoreCase);
        if (!isOwner && !isCreator)
            return ServiceResult.Failure("Bu kaynağı yalnız ekleyen kişi veya proje sahibi silebilir.");

        // Klasörse tüm alt ağacı topla; değilse sadece kendisi.
        var toDelete = entity.Type == ResourceType.Folder
            ? await CollectSubtreeAsync(projectId, resourceId, cancellationToken)
            : new List<ProjectResource> { entity };

        foreach (var r in toDelete)
        {
            if (r.Type == ResourceType.File)
                await _fileStorage.DeleteAsync(r.Url, cancellationToken);
            _resourceRepository.Remove(r);
        }

        await _resourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IReadOnlyList<ProjectResourceDto>>> GetTaskResourcesAsync(int taskId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Failure("Authentication required.");

        if (!await HasTaskAccessAsync(taskId, uid, cancellationToken))
            return ServiceResult<IReadOnlyList<ProjectResourceDto>>.NotFound($"Task {taskId} not found.");

        var resources = await _taskResourceRepository.Query()
            .Where(tr => tr.TaskId == taskId)
            .Include(tr => tr.Resource)
            .OrderByDescending(tr => tr.CreatedAt)
            .Select(tr => tr.Resource!)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<ProjectResourceDto>>.Success(resources.Select(ToDto).ToList());
    }

    public async Task<ServiceResult> LinkToTaskAsync(int taskId, int resourceId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult.Failure("Authentication required.");

        var task = await _taskRepository.Query()
            .Include(t => t.Project).ThenInclude(p => p!.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task?.Project is null || (task.Project.OwnerId != uid && !task.Project.Members.Any(m => m.UserId == uid)))
            return ServiceResult.NotFound($"Task {taskId} not found.");

        var resource = await _resourceRepository.Query()
            .FirstOrDefaultAsync(r => r.Id == resourceId && r.ProjectId == task.ProjectId, cancellationToken);
        if (resource is null)
            return ServiceResult.NotFound($"Resource {resourceId} not found.");

        var alreadyLinked = await _taskResourceRepository.Query()
            .AnyAsync(tr => tr.TaskId == taskId && tr.ResourceId == resourceId, cancellationToken);
        if (alreadyLinked)
            return ServiceResult.Success(); // idempotent

        await _taskResourceRepository.AddAsync(new TaskResource { TaskId = taskId, ResourceId = resourceId }, cancellationToken);
        await _taskResourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UnlinkFromTaskAsync(int taskId, int resourceId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult.Failure("Authentication required.");

        if (!await HasTaskAccessAsync(taskId, uid, cancellationToken))
            return ServiceResult.NotFound($"Task {taskId} not found.");

        var link = await _taskResourceRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(tr => tr.TaskId == taskId && tr.ResourceId == resourceId, cancellationToken);
        if (link is null)
            return ServiceResult.Success();

        _taskResourceRepository.Remove(link);
        await _taskResourceRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    // --- yardımcılar ---

    private async Task<bool> HasAccessAsync(int projectId, int uid, CancellationToken cancellationToken)
        => await _projectRepository.Query()
            .AnyAsync(p => p.Id == projectId && (p.OwnerId == uid || p.Members.Any(m => m.UserId == uid)), cancellationToken);

    private async Task<bool> HasTaskAccessAsync(int taskId, int uid, CancellationToken cancellationToken)
        => await _taskRepository.Query()
            .AnyAsync(t => t.Id == taskId && t.Project != null &&
                (t.Project.OwnerId == uid || t.Project.Members.Any(m => m.UserId == uid)), cancellationToken);

    private async Task<bool> IsValidParentAsync(int projectId, int? parentFolderId, CancellationToken cancellationToken)
    {
        if (parentFolderId is null) return true; // kök
        return await _resourceRepository.Query()
            .AnyAsync(r => r.Id == parentFolderId && r.ProjectId == projectId && r.Type == ResourceType.Folder, cancellationToken);
    }

    /// <summary>candidateParentId, folderId'nin alt ağacında mı? (taşıma döngüsü kontrolü)</summary>
    private async Task<bool> IsDescendantAsync(int projectId, int folderId, int? candidateParentId, CancellationToken cancellationToken)
    {
        var current = candidateParentId;
        while (current is not null)
        {
            if (current == folderId) return true;
            current = await _resourceRepository.Query()
                .Where(r => r.Id == current && r.ProjectId == projectId)
                .Select(r => r.ParentFolderId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        return false;
    }

    private async Task<List<ProjectResource>> CollectSubtreeAsync(int projectId, int rootId, CancellationToken cancellationToken)
    {
        var all = await _resourceRepository.Query(asNoTracking: false)
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var byParent = all.ToLookup(r => r.ParentFolderId);
        var result = new List<ProjectResource>();
        var stack = new Stack<int>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var node = all.FirstOrDefault(r => r.Id == id);
            if (node is null) continue;
            result.Add(node);
            foreach (var child in byParent[id]) stack.Push(child.Id);
        }
        return result;
    }

    private static ProjectResourceDto ToDto(ProjectResource r) => new(
        r.Id, r.ProjectId, r.ParentFolderId, r.Type, r.Title, r.Url, r.FileName, r.ContentType, r.SizeBytes,
        r.Type == ResourceType.YouTube ? ExtractYouTubeId(r.Url) : null,
        r.CreatedAt, r.CreatedBy);

    private static string? ExtractYouTubeId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = YouTubeIdRegex.Match(url);
        return m.Success ? m.Groups[1].Value : null;
    }
}
