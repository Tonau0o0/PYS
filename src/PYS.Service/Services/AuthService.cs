using Microsoft.EntityFrameworkCore;
using PYS.Core.Abstractions;
using PYS.Core.Common;
using PYS.Core.Entities;
using PYS.Service.Common;
using PYS.Service.DTOs.Auth;
using PYS.Service.Interfaces;

namespace PYS.Service.Services;

public sealed class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<ProjectInvitation> _invitationRepository;
    private readonly IRepository<ProjectMember> _memberRepository;
    private readonly ITokenService _tokenService;

    public AuthService(
        IRepository<User> userRepository,
        IRepository<ProjectInvitation> invitationRepository,
        IRepository<ProjectMember> memberRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _memberRepository = memberRepository;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedUser = dto.UserName.Trim();
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var exists = await _userRepository.Query()
            .AnyAsync(u => u.UserName == normalizedUser || u.Email == normalizedEmail, cancellationToken);

        if (exists)
        {
            return ServiceResult<AuthResponseDto>.Conflict("UserName or Email is already in use.");
        }

        var entity = new User
        {
            UserName = normalizedUser,
            Email = normalizedEmail,
            FullName = dto.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Member,
            IsActive = true,
            ColorHex = ColorPalette.PickFor(normalizedEmail)
        };

        await _userRepository.AddAsync(entity, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await AcceptPendingInvitationsAsync(entity.Id, normalizedEmail, cancellationToken);

        return ServiceResult<AuthResponseDto>.Success(BuildAuthResponse(entity));
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var userName = dto.UserName.Trim();

        var user = await _userRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return ServiceResult<AuthResponseDto>.Failure("Invalid credentials.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return ServiceResult<AuthResponseDto>.Failure("Invalid credentials.");
        }

        await AcceptPendingInvitationsAsync(user.Id, user.Email, cancellationToken);

        return ServiceResult<AuthResponseDto>.Success(BuildAuthResponse(user));
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult.NotFound($"User {userId} not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return ServiceResult.ValidationFailed(new[] { "Current password is incorrect." });
        }

        if (string.Equals(dto.CurrentPassword, dto.NewPassword, StringComparison.Ordinal))
        {
            return ServiceResult.ValidationFailed(new[] { "New password must differ from current password." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private async Task AcceptPendingInvitationsAsync(int userId, string email, CancellationToken cancellationToken)
    {
        var pending = await _invitationRepository.Query(asNoTracking: false)
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        foreach (var inv in pending)
        {
            var alreadyMember = await _memberRepository.Query()
                .AnyAsync(m => m.ProjectId == inv.ProjectId && m.UserId == userId, cancellationToken);

            if (!alreadyMember)
            {
                await _memberRepository.AddAsync(new ProjectMember
                {
                    ProjectId = inv.ProjectId,
                    UserId = userId,
                    Role = ProjectRole.Member,
                    JoinedAt = DateTime.UtcNow
                }, cancellationToken);
            }

            inv.Status = InvitationStatus.Accepted;
            inv.AcceptedAt = DateTime.UtcNow;
            _invitationRepository.Update(inv);
        }

        await _invitationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<ServiceResult> UpdateColorAsync(int userId, string colorHex, CancellationToken cancellationToken = default)
    {
        if (!ColorPalette.IsValid(colorHex))
        {
            return ServiceResult.ValidationFailed(new[] { "Color must be a valid hex value (e.g. #2196F3)." });
        }

        var user = await _userRepository.Query(asNoTracking: false)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return ServiceResult.NotFound($"User {userId} not found.");
        }

        user.ColorHex = colorHex;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var (token, expiresAt) = _tokenService.GenerateAccessToken(user);
        return new AuthResponseDto(token, expiresAt, user.Id, user.UserName, user.FullName, user.Role, user.ColorHex);
    }
}
