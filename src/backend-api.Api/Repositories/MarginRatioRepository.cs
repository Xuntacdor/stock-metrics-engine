using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class MarginRatioRepository : IMarginRatioRepository
{
    private readonly QuantIQContext _context;

    public MarginRatioRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<MarginRatio?> GetActiveBySymbolAsync(string symbol)
    {
        return await _context.MarginRatios
            .Where(m => m.Symbol == symbol &&
                        (m.ExpiredDate == null || m.ExpiredDate > DateTime.UtcNow))
            .OrderByDescending(m => m.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<MarginRatio>> GetAllActiveAsync()
    {
        return await _context.MarginRatios
            .Where(m => m.ExpiredDate == null || m.ExpiredDate > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task AddAsync(MarginRatio ratio)
    {
        await _context.MarginRatios.AddAsync(ratio);
    }

    public void Update(MarginRatio ratio)
    {
        _context.MarginRatios.Update(ratio);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
