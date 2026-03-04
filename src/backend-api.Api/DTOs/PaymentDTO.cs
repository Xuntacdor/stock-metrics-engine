namespace backend_api.Api.DTOs;

// ─── Deposit Request / Response ────────────────────────────────────────────────

/// <summary>User gửi request này để bắt đầu luồng nạp tiền.</summary>
public class CreateDepositRequest
{
    /// <summary>Số tiền nạp (VNĐ). Tối thiểu 1.000 VNĐ.</summary>
    public decimal Amount { get; set; }

    /// <summary>URL trang "Thành công" sau khi thanh toán xong (do Frontend cung cấp).</summary>
    public string ReturnUrl { get; set; } = null!;

    /// <summary>URL trang "Huỷ thanh toán" nếu user bấm quay lại (do Frontend cung cấp).</summary>
    public string CancelUrl { get; set; } = null!;
}

/// <summary>Hệ thống trả về sau khi tạo link thanh toán thành công.</summary>
public class CreateDepositResponse
{
    public long DepositId { get; set; }
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Redirect user đến URL này để tiến hành thanh toán trên PayOS.</summary>
    public string CheckoutUrl { get; set; } = null!;

    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
}

/// <summary>Thông tin chi tiết một lần nạp tiền.</summary>
public class DepositDetailResponse
{
    public long DepositId { get; set; }
    public string UserId { get; set; } = null!;
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

// ─── PayOS Webhook Payload ────────────────────────────────────────────────────
// Cấu trúc này theo định dạng webhook của PayOS .NET SDK (WebhookData)
// Không cần tự định nghĩa vì PayOS SDK đã có sẵn kiểu WebhookData.
// File này để dành cho các DTO nội bộ khác nếu cần.
