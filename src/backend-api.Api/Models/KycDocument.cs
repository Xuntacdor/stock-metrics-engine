namespace backend_api.Api.Models;

/// <summary>
/// Lưu trữ thông tin KYC (Know Your Customer) từ kết quả OCR CCCD.
/// Mỗi user có thể nộp nhiều lần (chỉ 1 bản APPROVED là hợp lệ).
/// </summary>
public class KycDocument
{
    public int KycId { get; set; }

    /// <summary>Foreign key reference đến Users.</summary>
    public string UserId { get; set; } = null!;

    // ─── Thông tin trích xuất từ FPT.AI ───────────────────────────────────

    /// <summary>Số CMND/CCCD (9 hoặc 12 số).</summary>
    public string? CardNumber { get; set; }

    public string? FullName { get; set; }

    /// <summary>Ngày sinh dạng string như FPT.AI trả về (vd: "01/01/1990").</summary>
    public string? DateOfBirth { get; set; }

    public string? Sex { get; set; }

    public string? Nationality { get; set; }

    /// <summary>Quê quán (home field).</summary>
    public string? HomeTown { get; set; }

    public string? Address { get; set; }

    /// <summary>Ngày hết hạn (doe) – chỉ có trên CCCD 12 số.</summary>
    public string? ExpiryDate { get; set; }

    /// <summary>
    /// Loại thẻ trả về bởi FPT.AI:
    /// old | old_back | new | new_back
    /// </summary>
    public string? CardType { get; set; }

    // ─── Thông tin upload ─────────────────────────────────────────────────

    /// <summary>Đường dẫn vật lý file ảnh đã lưu (wwwroot/kyc/...).</summary>
    public string ImagePath { get; set; } = null!;

    /// <summary>
    /// Trạng thái duyệt KYC:
    /// PENDING | APPROVED | REJECTED
    /// </summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>Lý do từ chối (nếu REJECTED).</summary>
    public string? RejectReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Navigation property.</summary>
    public virtual User? User { get; set; }
}
