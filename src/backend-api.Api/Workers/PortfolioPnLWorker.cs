using backend_api.Api.Data;
using backend_api.Api.Metrics;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Workers;

/// <summary>
/// Samples unrealised P&L for every portfolio holding every 5 minutes and records
/// it in the quantiq_portfolio_pnl_vnd Prometheus histogram.
///
/// The histogram is a population snapshot (not per-user), useful for dashboards
/// showing the distribution of gains/losses across the user base.
///
/// P&L is approximated as (AvgCostPrice – latest candle close) × TotalQuantity.
/// </summary>
public class PortfolioPnLWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PortfolioPnLWorker> _logger;

    private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(5);

    public PortfolioPnLWorker(IServiceScopeFactory scopeFactory, ILogger<PortfolioPnLWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PortfolioPnLWorker started (interval: {Min} min).", SampleInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(SampleInterval, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            try { await SampleAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "PortfolioPnLWorker: error during sampling."); }
        }
    }

    private async Task SampleAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<QuantIQContext>();

        // Pull all holdings with a non-zero quantity
        var holdings = await ctx.Portfolios
            .Where(p => p.TotalQuantity > 0 && p.AvgCostPrice > 0)
            .Select(p => new { p.Symbol, p.TotalQuantity, p.AvgCostPrice })
            .ToListAsync(ct);

        if (holdings.Count == 0) return;

        // Get the latest closing price for each symbol in a single query
        var symbols = holdings.Select(h => h.Symbol).Distinct().ToList();

        var latestCloses = await ctx.Candles
            .Where(c => symbols.Contains(c.Symbol) && c.Close != null)
            .GroupBy(c => c.Symbol)
            .Select(g => new
            {
                Symbol = g.Key,
                LatestClose = g.OrderByDescending(c => c.Timestamp).Select(c => c.Close).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Symbol, x => x.LatestClose, ct);

        int recorded = 0;
        foreach (var h in holdings)
        {
            if (!latestCloses.TryGetValue(h.Symbol, out var close) || close == null) continue;

            var pnl = (close.Value - h.AvgCostPrice) * h.TotalQuantity;
            AppMetrics.PortfolioPnLVnd.Observe((double)pnl);
            recorded++;
        }

        _logger.LogDebug("PortfolioPnLWorker: sampled P&L for {Count} holdings.", recorded);
    }
}
