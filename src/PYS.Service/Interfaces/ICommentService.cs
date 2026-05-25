using PYS.Service.Common;
using PYS.Service.DTOs.Tasks;

namespace PYS.Service.Interfaces;

public interface ICommentService
{
    Task<ServiceResult<IReadOnlyList<TaskCommentDto>>> GetForTaskAsync(int taskId, CancellationToken cancellationToken = default);
    Task<ServiceResult<TaskCommentDto>> AddAsync(int taskId, AddCommentDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int commentId, CancellationToken cancellationToken = default);
}
