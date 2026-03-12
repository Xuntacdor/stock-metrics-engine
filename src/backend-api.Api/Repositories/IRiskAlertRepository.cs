using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IRiskAlertRepository
{
    Task<IEnumerable<RiskAlert>> GetByUserIdAsync(string userId, int limit = 50);
    Task AddAsync(RiskAlert alert);
    Task SaveChangesAsync();
}
