using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;

namespace backend_api.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepo, ITokenService tokenService)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepo.GetByUsernameAsync(request.Username) != null)
            throw new InvalidOperationException("Username already exists.");

        if (await _userRepo.GetByEmailAsync(request.Email) != null)
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.GetByUsernameAsync(request.Username)
            ?? throw new UnauthorizedAccessException("Username or password is not correct.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Username or password is not correct.");

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);
        await _userRepo.SaveChangesAsync();

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt,
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await _userRepo.GetByRefreshTokenAsync(refreshToken) 
                   ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired. Please login again.");
        }

        var (newToken, expiresAt) = _tokenService.GenerateToken(user);

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);

        
       
        await _userRepo.SaveChangesAsync();

        return new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            ExpiresAt = expiresAt,
        };
    }
}