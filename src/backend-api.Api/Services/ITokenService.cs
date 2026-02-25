using backend_api.Api.Models;

namespace backend_api.Api.Services;

public interface ITokenService
{
    (string token, DateTime expiresAt) GenerateToken(User user);
    string GenerateRefreshToken();
}