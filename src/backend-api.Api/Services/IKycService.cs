using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IKycService
{
    /// <summary>
    /// User upload ảnh CCCD:
    /// 1. Lưu file vào wwwroot/kyc
    /// 2. Gọi FPT.AI OCR API
    /// 3. Lưu kết quả vào DB với trạng thái PENDING
    /// </summary>
    Task<KycUploadResponse> UploadAndOcrAsync(string userId, IFormFile image);

    /// <summary>Lấy danh sách KYC của một user.</summary>
    Task<IEnumerable<KycDetailResponse>> GetByUserIdAsync(string userId);

    /// <summary>Lấy tất cả KYC PENDING để Admin duyệt.</summary>
    Task<IEnumerable<KycDetailResponse>> GetPendingAsync();

    /// <summary>Admin duyệt (APPROVED) hoặc từ chối (REJECTED) một bản KYC.</summary>
    Task<KycDetailResponse> ReviewAsync(int kycId, KycReviewRequest request);
}
