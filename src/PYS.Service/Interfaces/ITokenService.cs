using PYS.Core.Entities;

namespace PYS.Service.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
}
