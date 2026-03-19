using backend_api.Api.DTOs;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;

    public AuthController(IAuthService authService, ICacheService cacheService)
    {
        _authService = authService;
        _cacheService = cacheService;
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

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(30),
                Secure = false,        
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);
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
    public async Task<IActionResult> Logout(){
        string authHeader = Request.Headers["Authorization"].ToString();
        string token = authHeader.Replace("Bearer ", "");
        var expiry = TimeSpan.FromMinutes(60);
        await _cacheService.SetTokenBlacklistAsync(token, expiry);
        return Ok(new { message = "Logout successful" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token not found. Please login again." });
        }

        try
        {
            var response = await _authService.RefreshTokenAsync(refreshToken);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(30),
                Secure = false,        // false để hoạt động với HTTP localhost
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

            response.RefreshToken = ""; 

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
