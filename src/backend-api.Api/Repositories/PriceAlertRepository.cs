using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class PriceAlertRepository : IPriceAlertRepository
{
    private readonly QuantIQContext _ctx;
    public PriceAlertRepository(QuantIQContext ctx) => _ctx = ctx;

    public Task<List<PriceAlert>> GetByUserIdAsync(string userId) =>
        _ctx.PriceAlerts.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public Task<PriceAlert?> GetByIdAsync(int alertId) =>
        _ctx.PriceAlerts.FindAsync(alertId).AsTask();

    /// <summary>All active alerts that have not been triggered (or are not NotifyOnce).</summary>
    public Task<List<PriceAlert>> GetActiveAlertsAsync() =>
        _ctx.PriceAlerts
            .Where(a => a.IsActive && (!a.IsTriggered || !a.NotifyOnce))
            .ToListAsync();

    public async Task AddAsync(PriceAlert alert) => await _ctx.PriceAlerts.AddAsync(alert);
    public void Update(PriceAlert alert) => _ctx.PriceAlerts.Update(alert);
    public void Delete(PriceAlert alert) => _ctx.PriceAlerts.Remove(alert);
    public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
}
