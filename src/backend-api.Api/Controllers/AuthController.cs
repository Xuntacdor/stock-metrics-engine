using backend_api.Api.DTOs;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IdentityModel.Tokens.Jwt;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService authService, ICacheService cacheService, IWebHostEnvironment env)
    {
        _authService = authService;
        _cacheService = cacheService;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            Response.Cookies.Append("refreshToken", response.RefreshToken, BuildRefreshTokenCookieOptions());
            response.RefreshToken = "";
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var token = authHeader.Replace("Bearer ", "");

        var expiry = ParseRemainingLifetime(token);
        await _cacheService.SetTokenBlacklistAsync(token, expiry);

        return Ok(new { message = "Logout successful" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token not found. Please login again." });

        try
        {
            var response = await _authService.RefreshTokenAsync(refreshToken);
            Response.Cookies.Append("refreshToken", response.RefreshToken, BuildRefreshTokenCookieOptions());
            response.RefreshToken = "";
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses the JWT's own expiry claim and returns how long the token still has to live.
    /// Falls back to 5 minutes if the token cannot be read (already expired or malformed).
    /// </summary>
    private static TimeSpan ParseRemainingLifetime(string rawToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(rawToken);
            var remaining = jwt.ValidTo.ToUniversalTime() - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMinutes(5);
        }
        catch
        {
            return TimeSpan.FromMinutes(5);
        }
    }

    /// <summary>
    /// Builds cookie options for the refresh-token cookie.
    /// Secure is enabled in all environments except Development so that the cookie
    /// is not transmitted over plain HTTP in production/staging.
    /// </summary>
    private CookieOptions BuildRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Expires  = DateTime.UtcNow.AddDays(30),
        Secure   = !_env.IsDevelopment(),
        SameSite = SameSiteMode.Lax
    };
}
