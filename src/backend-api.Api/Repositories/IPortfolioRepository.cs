using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IPortfolioRepository
{
    Task<Portfolio?> GetByUserAndSymbolAsync(string userId, string symbol);
    Task<IEnumerable<Portfolio>> GetByUserIdAsync(string userId);

    Task<IEnumerable<Portfolio>> GetBySymbolAsync(string symbol);

    Task AddAsync(Portfolio portfolio);
    void Update(Portfolio portfolio);
    Task SaveChangesAsync();
}
