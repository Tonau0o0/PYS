using PYS.Service.Common;
using PYS.Service.DTOs.Auth;

namespace PYS.Service.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateColorAsync(int userId, string colorHex, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponseDto>> UpdateProfileAsync(int userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponseDto>> UpdateAvatarAsync(int userId, Stream content, string fileName, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponseDto>> UpdateEmailAsync(int userId, UpdateEmailDto dto, CancellationToken cancellationToken = default);
}
