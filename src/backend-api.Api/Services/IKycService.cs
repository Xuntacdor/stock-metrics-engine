using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IKycService
{
  
    Task<KycUploadResponse> UploadAndOcrAsync(string userId, IFormFile image);

    Task<IEnumerable<KycDetailResponse>> GetByUserIdAsync(string userId);

    Task<IEnumerable<KycDetailResponse>> GetPendingAsync();

    Task<KycDetailResponse> ReviewAsync(int kycId, KycReviewRequest request);
}
