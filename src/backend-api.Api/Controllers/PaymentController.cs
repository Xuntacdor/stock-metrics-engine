using System.Security.Claims;
using backend_api.Api.DTOs;
using backend_api.Api.Filters;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger         = logger;
    }

    
    [HttpPost("deposit")]
    [RequireActiveAccount]
    public async Task<IActionResult> CreateDeposit([FromBody] CreateDepositRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var result = await _paymentService.CreateDepositLinkAsync(userId, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create deposit link for user {UserId}", userId);
            return StatusCode(500, new { message = "Không thể tạo link thanh toán. Vui lòng thử lại." });
        }
    }

    
    [HttpGet("deposits")]
    public async Task<IActionResult> GetDepositHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _paymentService.GetDepositHistoryAsync(userId);
        return Ok(result);
    }

  
    [HttpGet("cancel")]
    [AllowAnonymous]  
    public async Task<IActionResult> CancelDeposit([FromQuery] long orderCode)
    {
        if (orderCode <= 0)
            return BadRequest(new { message = "OrderCode không hợp lệ." });

        await _paymentService.CancelDepositAsync(orderCode);
        return Ok(new { message = "Lệnh nạp tiền đã được huỷ.", orderCode });
    }

    
    [HttpPost("webhook")]
    [AllowAnonymous]  
    public async Task<IActionResult> PayOsWebhook()
    {
       
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

       
        var signature = Request.Headers["X-PayOS-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await _paymentService.HandleWebhookAsync(rawBody, signature);
            return Ok("OK");  
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Invalid PayOS webhook signature received.");
            return BadRequest("Invalid signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
           
            return Ok("ERROR_LOGGED");
        }
    }
}
