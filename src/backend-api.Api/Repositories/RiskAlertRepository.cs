using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class RiskAlertRepository : IRiskAlertRepository
{
    private readonly QuantIQContext _context;

    public RiskAlertRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RiskAlert>> GetByUserIdAsync(string userId, int limit = 50)
    {
        return await _context.RiskAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task AddAsync(RiskAlert alert)
    {
        await _context.RiskAlerts.AddAsync(alert);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
