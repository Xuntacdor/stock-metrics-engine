using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(string userId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    
}