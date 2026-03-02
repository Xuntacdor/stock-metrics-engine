using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class CorporateActionRepository : ICorporateActionRepository
{
    private readonly QuantIQContext _context;

    public CorporateActionRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CorporateAction>> GetAllAsync()
    {
        return await _context.CorporateActions
            .OrderByDescending(a => a.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<CorporateAction>> GetBySymbolAsync(string symbol)
    {
        return await _context.CorporateActions
            .Where(a => a.Symbol == symbol)
            .OrderByDescending(a => a.PaymentDate)
            .ToListAsync();
    }

    public async Task<CorporateAction?> GetByIdAsync(int actionId)
    {
        return await _context.CorporateActions
            .FirstOrDefaultAsync(a => a.ActionId == actionId);
    }

    public async Task<IEnumerable<CorporateAction>> GetPendingForTodayAsync(DateTime today)
    {
        var todayDate = today.Date;
        return await _context.CorporateActions
            .Where(a => a.PaymentDate.Date == todayDate && a.Status == "PENDING")
            .ToListAsync();
    }

    public async Task AddAsync(CorporateAction action)
    {
        await _context.CorporateActions.AddAsync(action);
    }

    public void Update(CorporateAction action)
    {
        _context.CorporateActions.Update(action);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
