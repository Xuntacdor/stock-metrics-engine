using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly QuantIQContext _context;

    public PortfolioRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<Portfolio?> GetByUserAndSymbolAsync(string userId, string symbol)
    {
        return await _context.Portfolios
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Symbol == symbol);
    }

    public async Task<IEnumerable<Portfolio>> GetByUserIdAsync(string userId)
    {
        return await _context.Portfolios
            .Where(p => p.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(Portfolio portfolio)
    {
        await _context.Portfolios.AddAsync(portfolio);
    }

    public void Update(Portfolio portfolio)
    {
        _context.Portfolios.Update(portfolio);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
