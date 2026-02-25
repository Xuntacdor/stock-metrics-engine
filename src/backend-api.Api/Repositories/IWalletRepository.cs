using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IWalletRepository
{
    Task<CashWallet?> GetByUserIdAsync(string userId);
    Task AddAsync(CashWallet wallet);
    void Update(CashWallet wallet);
    Task SaveChangesAsync();
}
