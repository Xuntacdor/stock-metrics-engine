using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface ICorporateActionRepository
{
    Task<IEnumerable<CorporateAction>> GetAllAsync();
    Task<IEnumerable<CorporateAction>> GetBySymbolAsync(string symbol);
    Task<CorporateAction?> GetByIdAsync(int actionId);


    Task<IEnumerable<CorporateAction>> GetPendingForTodayAsync(DateTime today);

    Task AddAsync(CorporateAction action);
    void Update(CorporateAction action);
    Task SaveChangesAsync();
}
