namespace backend_api.Api.DTOs;

// ─── Response từ FPT.AI (raw) ───────────────────────────────────────────────

/// <summary>Kết quả OCR mặt trước CCCD mới (12 số) hoặc CMND.</summary>
public class FptAiOcrData
{
    public string? id { get; set; }
    public string? id_prob { get; set; }
    public string? name { get; set; }
    public string? name_prob { get; set; }
    public string? dob { get; set; }
    public string? dob_prob { get; set; }
    public string? sex { get; set; }
    public string? sex_prob { get; set; }
    public string? nationality { get; set; }
    public string? nationality_prob { get; set; }
    public string? home { get; set; }
    public string? home_prob { get; set; }
    public string? address { get; set; }
    public string? address_prob { get; set; }
    public string? doe { get; set; }
    public string? doe_prob { get; set; }
    public string? type { get; set; }
    public string? type_new { get; set; }
    public FptAiAddressEntities? address_entities { get; set; }
}

public class FptAiAddressEntities
{
    public string? province { get; set; }
    public string? district { get; set; }
    public string? ward { get; set; }
    public string? street { get; set; }
}

public class FptAiOcrResponse
{
    public int errorCode { get; set; }
    public string? errorMessage { get; set; }
    public List<FptAiOcrData>? data { get; set; }
}

// ─── API Request / Response của hệ thống ────────────────────────────────────

/// <summary>
/// Response trả về cho client sau khi upload ảnh CCCD thành công.
/// Bao gồm kết quả OCR đã trích xuất.
/// </summary>
public class KycUploadResponse
{
    public int KycId { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? CardNumber { get; set; }
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? Nationality { get; set; }
    public string? HomeTown { get; set; }
    public string? Address { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CardType { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Thông tin KYC của một user (dùng cho Admin hoặc user tự xem).</summary>
public class KycDetailResponse
{
    public int KycId { get; set; }
    public string UserId { get; set; } = null!;
    public string? CardNumber { get; set; }
    public string? FullName { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Sex { get; set; }
    public string? Nationality { get; set; }
    public string? HomeTown { get; set; }
    public string? Address { get; set; }
    public string? ExpiryDate { get; set; }
    public string? CardType { get; set; }
    public string ImagePath { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RejectReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Admin dùng request này để duyệt hoặc từ chối KYC.</summary>
public class KycReviewRequest
{
    /// <summary>APPROVED hoặc REJECTED.</summary>
    public string Decision { get; set; } = null!;

    /// <summary>Bắt buộc khi Decision = REJECTED.</summary>
    public string? RejectReason { get; set; }
}
