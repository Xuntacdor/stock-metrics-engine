using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;


public class UserRepository : IUserRepository
{
    private readonly QuantIQContext _context;

    public UserRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username)
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdAsync(string userId)
        => await _context.Users.FindAsync(userId);

    public async Task AddAsync(User user)
        => await _context.Users.AddAsync(user);

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
    
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        => await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
}
