using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IPaymentService
{
    /// <summary>
    /// Tạo link thanh toán PayOS cho lệnh nạp tiền.
    /// Lưu DepositRequest vào DB với trạng thái PENDING và trả về CheckoutUrl.
    /// </summary>
    Task<CreateDepositResponse> CreateDepositLinkAsync(string userId, CreateDepositRequest request);

    /// <summary>
    /// Xử lý webhook từ PayOS khi thanh toán thành công.
    /// Đảm bảo idempotency: không cộng tiền 2 lần cho 1 OrderCode.
    /// </summary>
    Task HandleWebhookAsync(string rawBody, string payosSignature);

    /// <summary>Lấy danh sách lịch sử nạp tiền của user.</summary>
    Task<IEnumerable<DepositDetailResponse>> GetDepositHistoryAsync(string userId);

    /// <summary>Cập nhật trạng thái CANCELLED khi user huỷ thanh toán.</summary>
    Task CancelDepositAsync(long orderCode);
}
