namespace backend_api.Api.Models;

/// <summary>
/// Lưu trữ mỗi lệnh nạp tiền qua PayOS.
/// Đây là bản ghi "phiếu nạp" — được tạo ngay khi user yêu cầu nạp,
/// và chỉ coi là hoàn tất khi nhận được webhook PAID từ PayOS.
/// </summary>
public class DepositRequest
{
    public long DepositId { get; set; }

    /// <summary>Foreign key → Users</summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Mã đơn hàng gửi sang PayOS (OrderCode).
    /// Là số nguyên dương, unique, max 9999999999999 (~14 chữ số).
    /// Dùng Unix timestamp ms để đảm bảo unique.
    /// </summary>
    public long OrderCode { get; set; }

    /// <summary>Số tiền nạp (VNĐ), tối thiểu 1000.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Trạng thái:
    /// PENDING   - đã tạo link, chờ user thanh toán
    /// PAID      - webhook PAID nhận được, đã cộng tiền
    /// CANCELLED - user huỷ hoặc hết thời gian
    /// </summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>Checkout URL của PayOS, redirect user đến đây.</summary>
    public string? CheckoutUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    /// <summary>Navigation property</summary>
    public virtual User? User { get; set; }
}
