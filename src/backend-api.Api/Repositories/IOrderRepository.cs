using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string orderId);
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    Task AddAsync(Order order);
    void Update(Order order);
    Task SaveChangesAsync();
}
