namespace backend_api.Api.DTOs;


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

public class KycReviewRequest
{
    public string Decision { get; set; } = null!;

    public string? RejectReason { get; set; }
}

/// <summary>
/// Thông tin profile tổng hợp của user hiện tại.
/// Trả về từ GET /api/me
/// </summary>
public class UserProfileResponse
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public DateTime? CreatedAt { get; set; }

    // ─── Trạng thái tài khoản & KYC ──────────────────────────────────────

    /// <summary>INACTIVE | ACTIVE | SUSPENDED</summary>
    public string AccountStatus { get; set; } = null!;

    /// <summary>PENDING | APPROVED | REJECTED</summary>
    public string KycStatus { get; set; } = null!;

    /// <summary>Hướng dẫn bước tiếp theo cho user dựa trên trạng thái hiện tại.</summary>
    public string NextStep { get; set; } = null!;

    /// <summary>Thông tin bản KYC mới nhất (nếu có).</summary>
    public LatestKycInfo? LatestKyc { get; set; }
}

public class LatestKycInfo
{
    public int KycId { get; set; }
    public string Status { get; set; } = null!;
    public string? RejectReason { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Admin suspend/unsuspend một tài khoản.</summary>
public class SuspendAccountRequest
{
    /// <summary>SUSPENDED hoặc ACTIVE.</summary>
    public string AccountStatus { get; set; } = null!;

    public string? Reason { get; set; }
}
