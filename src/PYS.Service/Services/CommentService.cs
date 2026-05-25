using Microsoft.EntityFrameworkCore;
using PYS.Core.Abstractions;
using PYS.Core.Entities;
using PYS.Service.Common;
using PYS.Service.DTOs.Tasks;
using PYS.Service.Interfaces;

namespace PYS.Service.Services;

public sealed class CommentService : ICommentService
{
    private readonly IRepository<TaskComment> _commentRepository;
    private readonly IRepository<ProjectTask> _taskRepository;
    private readonly ICurrentUserService _currentUser;

    public CommentService(
        IRepository<TaskComment> commentRepository,
        IRepository<ProjectTask> taskRepository,
        ICurrentUserService currentUser)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<IReadOnlyList<TaskCommentDto>>> GetForTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<IReadOnlyList<TaskCommentDto>>.Failure("Authentication required.");

        if (!await HasTaskAccessAsync(taskId, uid, cancellationToken))
            return ServiceResult<IReadOnlyList<TaskCommentDto>>.NotFound($"Task {taskId} not found.");

        var data = await _commentRepository.Query()
            .Where(c => c.TaskId == taskId)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TaskCommentDto(
                c.Id, c.TaskId, c.Content, c.AuthorId,
                c.Author!.FullName, c.Author.ColorHex, c.Author.AvatarUrl, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<TaskCommentDto>>.Success(data);
    }

    public async Task<ServiceResult<TaskCommentDto>> AddAsync(int taskId, AddCommentDto dto, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult<TaskCommentDto>.Failure("Authentication required.");

        if (!await HasTaskAccessAsync(taskId, uid, cancellationToken))
            return ServiceResult<TaskCommentDto>.NotFound($"Task {taskId} not found.");

        var entity = new TaskComment
        {
            TaskId = taskId,
            AuthorId = uid,
            Content = dto.Content.Trim()
        };

        await _commentRepository.AddAsync(entity, cancellationToken);
        await _commentRepository.SaveChangesAsync(cancellationToken);

        // Yazar bilgisiyle birlikte döndür.
        var created = await _commentRepository.Query()
            .Include(c => c.Author)
            .Where(c => c.Id == entity.Id)
            .Select(c => new TaskCommentDto(
                c.Id, c.TaskId, c.Content, c.AuthorId,
                c.Author!.FullName, c.Author.ColorHex, c.Author.AvatarUrl, c.CreatedAt))
            .FirstAsync(cancellationToken);

        return ServiceResult<TaskCommentDto>.Success(created);
    }

    public async Task<ServiceResult> DeleteAsync(int commentId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not int uid)
            return ServiceResult.Failure("Authentication required.");

        var comment = await _commentRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);
        if (comment is null)
            return ServiceResult.NotFound($"Comment {commentId} not found.");

        if (!await HasTaskAccessAsync(comment.TaskId, uid, cancellationToken))
            return ServiceResult.NotFound($"Comment {commentId} not found.");

        // Yalnız yorumu yazan kişi silebilir.
        if (comment.AuthorId != uid)
            return ServiceResult.Failure("Bu yorumu yalnız yazarı silebilir.");

        _commentRepository.Remove(comment);
        await _commentRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<bool> HasTaskAccessAsync(int taskId, int uid, CancellationToken cancellationToken)
        => await _taskRepository.Query()
            .AnyAsync(t => t.Id == taskId && t.Project != null &&
                (t.Project.OwnerId == uid || t.Project.Members.Any(m => m.UserId == uid)), cancellationToken);
}
