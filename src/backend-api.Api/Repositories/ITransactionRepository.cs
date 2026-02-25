using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, string? transType = null);
    Task AddAsync(Transaction transaction);
    Task SaveChangesAsync();
}
