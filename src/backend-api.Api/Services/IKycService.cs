using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IKycService
{
    /// <summary>Upload ảnh CCCD → OCR → lưu DB (PENDING).</summary>
    Task<KycUploadResponse> UploadAndOcrAsync(string userId, IFormFile image);

    /// <summary>User xem toàn bộ lịch sử KYC của mình.</summary>
    Task<IEnumerable<KycDetailResponse>> GetByUserIdAsync(string userId);

    /// <summary>Lấy profile đầy đủ (AccountStatus, KycStatus, bản KYC mới nhất).</summary>
    Task<UserProfileResponse> GetMyProfileAsync(string userId);

    /// <summary>Admin lấy danh sách KYC PENDING.</summary>
    Task<IEnumerable<KycDetailResponse>> GetPendingAsync();

    /// <summary>Admin duyệt hoặc từ chối KYC → đồng bộ AccountStatus.</summary>
    Task<KycDetailResponse> ReviewAsync(int kycId, KycReviewRequest request);

    /// <summary>Admin khoá/mở khoá tài khoản (SUSPENDED / ACTIVE).</summary>
    Task<UserProfileResponse> SuspendAccountAsync(string targetUserId, SuspendAccountRequest request);
}
