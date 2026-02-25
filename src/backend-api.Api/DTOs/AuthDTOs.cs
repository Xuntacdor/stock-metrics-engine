namespace backend_api.Api.DTOs;

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime ExpiresAt { get; set; }
}
