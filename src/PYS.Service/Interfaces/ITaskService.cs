using PYS.Service.Common;
using PYS.Service.DTOs.Tasks;

namespace PYS.Service.Interfaces;

public interface ITaskService
{
    Task<ServiceResult<IReadOnlyList<TaskDto>>> GetAllAsync(TaskFilterDto? filter, CancellationToken cancellationToken = default);
    Task<ServiceResult<TaskDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<TaskDto>> CreateAsync(CreateTaskDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<TaskDto>> UpdateAsync(int id, UpdateTaskDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
