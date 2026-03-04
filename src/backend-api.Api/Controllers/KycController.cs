using System.Security.Claims;
using backend_api.Api.DTOs;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/kyc")]
[Authorize]
public class KycController : ControllerBase
{
    private readonly IKycService _kycService;

    public KycController(IKycService kycService)
    {
        _kycService = kycService;
    }

 
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] IFormFile image)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { message = "Không xác định được user." });

        try
        {
            var result = await _kycService.UploadAndOcrAsync(userId, image);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { message = "Lỗi kết nối tới dịch vụ OCR.", detail = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống.", detail = ex.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyKyc()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _kycService.GetByUserIdAsync(userId);
        return Ok(result);
    }

  
    [HttpGet("/api/me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _kycService.GetMyProfileAsync(userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

   
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _kycService.GetPendingAsync();
        return Ok(result);
    }

  
    [HttpPost("{kycId:int}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(int kycId, [FromBody] KycReviewRequest request)
    {
        try
        {
            var result = await _kycService.ReviewAsync(kycId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

   
    [HttpPost("/api/admin/users/{userId}/suspend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SuspendAccount(string userId, [FromBody] SuspendAccountRequest request)
    {
        try
        {
            var result = await _kycService.SuspendAccountAsync(userId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

   

    private string? GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
