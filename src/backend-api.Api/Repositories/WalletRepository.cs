using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly QuantIQContext _context;

    public WalletRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<CashWallet?> GetByUserIdAsync(string userId)
    {
        return await _context.CashWallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task AddAsync(CashWallet wallet)
    {
        await _context.CashWallets.AddAsync(wallet);
    }

    public void Update(CashWallet wallet)
    {
        _context.CashWallets.Update(wallet);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
