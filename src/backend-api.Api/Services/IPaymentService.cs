using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IPaymentService
{
    
    Task<CreateDepositResponse> CreateDepositLinkAsync(string userId, CreateDepositRequest request);

    
    Task HandleWebhookAsync(string rawBody, string payosSignature);


    Task<IEnumerable<DepositDetailResponse>> GetDepositHistoryAsync(string userId);

   
    Task CancelDepositAsync(long orderCode);
}
