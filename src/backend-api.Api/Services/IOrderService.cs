using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IOrderService
{
    Task<OrderResponse> PlaceOrderAsync(string userId, PlaceOrderRequest request);
    Task<IEnumerable<OrderResponse>> GetMyOrdersAsync(string userId);
    Task CancelOrderAsync(string userId, string orderId);
}
