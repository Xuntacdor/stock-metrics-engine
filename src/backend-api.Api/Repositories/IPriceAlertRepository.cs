using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IPriceAlertRepository
{
    Task<List<PriceAlert>> GetByUserIdAsync(string userId);
    Task<PriceAlert?> GetByIdAsync(int alertId);
    Task<List<PriceAlert>> GetActiveAlertsAsync();
    Task AddAsync(PriceAlert alert);
    void Update(PriceAlert alert);
    void Delete(PriceAlert alert);
    Task SaveChangesAsync();
}
