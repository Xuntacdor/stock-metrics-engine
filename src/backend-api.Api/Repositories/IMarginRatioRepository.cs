using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IMarginRatioRepository
{
    Task<MarginRatio?> GetActiveBySymbolAsync(string symbol);

    Task<IEnumerable<MarginRatio>> GetAllActiveAsync();

    Task AddAsync(MarginRatio ratio);
    void Update(MarginRatio ratio);
    Task SaveChangesAsync();
}
