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

    // ──────────────────────────────────────────────────────────────────────────
    // USER ENDPOINTS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo link thanh toán PayOS để nạp tiền vào ví.
    /// Response trả về CheckoutUrl — Frontend redirect user đến đó.
    ///
    /// Flow:
    ///   POST /api/payments/deposit → [system tạo OrderCode + gọi PayOS] → trả CheckoutUrl
    ///   User thanh toán trên trang PayOS → PayOS gọi webhook → hệ thống cộng tiền
    /// </summary>
    [HttpPost("deposit")]
    [RequireActiveAccount]  // Chỉ tài khoản đã KYC mới được nạp tiền
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

    /// <summary>
    /// User xem lịch sử các lần nạp tiền của mình.
    /// </summary>
    [HttpGet("deposits")]
    public async Task<IActionResult> GetDepositHistory()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _paymentService.GetDepositHistoryAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Endpoint callback khi user HUỶ thanh toán trên trang PayOS.
    /// PayOS redirect về: [CancelUrl]?orderCode={code}&cancel=true
    /// Frontend gọi endpoint này sau khi nhận redirect.
    /// </summary>
    [HttpGet("cancel")]
    [AllowAnonymous]  // Callback URL, không cần auth
    public async Task<IActionResult> CancelDeposit([FromQuery] long orderCode)
    {
        if (orderCode <= 0)
            return BadRequest(new { message = "OrderCode không hợp lệ." });

        await _paymentService.CancelDepositAsync(orderCode);
        return Ok(new { message = "Lệnh nạp tiền đã được huỷ.", orderCode });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // WEBHOOK ENDPOINT (PayOS → Server)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PayOS gọi endpoint này sau khi thanh toán hoàn tất.
    /// KHÔNG cần JWT — PayOS gọi trực tiếp từ server của họ.
    /// Bảo mật bằng checksum signature do PayOS SDK xác minh.
    ///
    /// Quan trọng:
    /// - Endpoint phải accessible từ internet (PayOS server gọi đến)
    /// - Phải luôn trả 200 OK để PayOS không retry vô hạn
    /// - Logic cộng tiền bên trong có idempotency check (không cộng 2 lần)
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]  // PayOS không gửi JWT, chỉ gửi signature trong body
    public async Task<IActionResult> PayOsWebhook()
    {
        // Đọc raw body để xác minh signature
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();

        // Chữ ký PayOS nằm trong body (không phải header) → SDK tự parse
        var signature = Request.Headers["X-PayOS-Signature"].FirstOrDefault() ?? string.Empty;

        try
        {
            await _paymentService.HandleWebhookAsync(rawBody, signature);
            return Ok("OK");  // PayOS cần nhận "OK" để dừng retry
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Invalid PayOS webhook signature received.");
            return BadRequest("Invalid signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS webhook");
            // Vẫn trả 200 để PayOS không retry — lỗi được log để dev xử lý
            return Ok("ERROR_LOGGED");
        }
    }
}
