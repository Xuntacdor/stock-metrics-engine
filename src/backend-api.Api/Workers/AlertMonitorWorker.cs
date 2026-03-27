using backend_api.Api.Constants;
using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Hubs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Workers;

/// <summary>
/// Background worker that checks user-defined PriceAlerts every 30 seconds.
/// When an alert condition is met it:
///   1. Marks the alert as triggered (and deactivates it if NotifyOnce).
///   2. Pushes an "AlertTriggered" event to the owning user via SignalR.
/// </summary>
public class AlertMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<MarketHub> _hub;
    private readonly ILogger<AlertMonitorWorker> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public AlertMonitorWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<MarketHub> hub,
        ILogger<AlertMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertMonitorWorker started (interval: {Interval}s).", CheckInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            try { await CheckAlertsAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "AlertMonitorWorker: unexpected error."); }
        }

        _logger.LogInformation("AlertMonitorWorker stopped.");
    }

    // ── Core check loop ───────────────────────────────────────────────────────

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var ctx       = scope.ServiceProvider.GetRequiredService<QuantIQContext>();
        var alertRepo = scope.ServiceProvider.GetRequiredService<IPriceAlertRepository>();

        var alerts = await alertRepo.GetActiveAlertsAsync();
        if (alerts.Count == 0) return;

        _logger.LogDebug("AlertMonitorWorker: evaluating {Count} active alert(s).", alerts.Count);

        // Group by symbol to avoid redundant DB queries
        var bySymbol = alerts.GroupBy(a => a.Symbol);

        foreach (var group in bySymbol)
        {
            if (ct.IsCancellationRequested) break;

            var symbol = group.Key;
            var latestClose = await GetLatestCloseAsync(ctx, symbol);
            var latestVolume = await GetLatestVolumeAsync(ctx, symbol);
            var rsi = await ComputeRsi14Async(ctx, symbol);

            foreach (var alert in group)
            {
                var currentValue = alert.AlertType switch
                {
                    AlertType.Price  => latestClose,
                    AlertType.Volume => latestVolume,
                    AlertType.Rsi    => rsi,
                    _                => latestClose
                };

                if (currentValue == null) continue;

                if (ConditionMet(alert.Condition, currentValue.Value, alert.ThresholdValue))
                    await FireAlertAsync(alert, currentValue.Value, alertRepo, ct);
            }
        }

        await alertRepo.SaveChangesAsync();
    }

    // ── Data helpers ──────────────────────────────────────────────────────────

    private static Task<decimal?> GetLatestCloseAsync(QuantIQContext ctx, string symbol) =>
        ctx.Candles
            .Where(c => c.Symbol == symbol && c.Close != null)
            .OrderByDescending(c => c.Timestamp)
            .Select(c => c.Close)
            .FirstOrDefaultAsync();

    private static Task<decimal?> GetLatestVolumeAsync(QuantIQContext ctx, string symbol) =>
        ctx.Candles
            .Where(c => c.Symbol == symbol && c.Volume != null)
            .OrderByDescending(c => c.Timestamp)
            .Select(c => (decimal?)c.Volume)
            .FirstOrDefaultAsync();

    /// <summary>Wilder's RSI-14 from the latest 15 daily closes.</summary>
    private static async Task<decimal?> ComputeRsi14Async(QuantIQContext ctx, string symbol)
    {
        var closes = await ctx.Candles
            .Where(c => c.Symbol == symbol && c.Close != null)
            .OrderByDescending(c => c.Timestamp)
            .Take(15)
            .Select(c => c.Close!.Value)
            .ToListAsync();

        if (closes.Count < 15) return null;

        // Reverse to chronological order
        closes.Reverse();

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < closes.Count; i++)
        {
            var diff = closes[i] - closes[i - 1];
            if (diff >= 0) { gains.Add(diff); losses.Add(0); }
            else           { gains.Add(0);    losses.Add(-diff); }
        }

        var avgGain = gains.Average();
        var avgLoss = losses.Average();

        if (avgLoss == 0) return 100m;

        var rs = avgGain / avgLoss;
        return Math.Round(100m - (100m / (1m + rs)), 2);
    }

    // ── Condition evaluation ──────────────────────────────────────────────────

    private static bool ConditionMet(string condition, decimal current, decimal threshold) =>
        condition switch
        {
            AlertCondition.Gt  => current >  threshold,
            AlertCondition.Gte => current >= threshold,
            AlertCondition.Lt  => current <  threshold,
            AlertCondition.Lte => current <= threshold,
            _                  => false
        };

    // ── Fire & notify ─────────────────────────────────────────────────────────

    private async Task FireAlertAsync(
        PriceAlert alert,
        decimal currentValue,
        IPriceAlertRepository alertRepo,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Alert #{AlertId} triggered for user {UserId}: {Symbol} {Type} {Cond} {Threshold} (current={Current})",
            alert.AlertId, alert.UserId, alert.Symbol, alert.AlertType, alert.Condition,
            alert.ThresholdValue, currentValue);

        alert.IsTriggered = true;
        alert.TriggeredAt = DateTime.UtcNow;

        if (alert.NotifyOnce)
            alert.IsActive = false;

        alertRepo.Update(alert);

        var notification = new AlertTriggeredNotification(
            alert.AlertId,
            alert.Symbol,
            alert.AlertType,
            alert.Condition,
            alert.ThresholdValue,
            currentValue,
            alert.TriggeredAt.Value);

        // Push to the owning user only — no other users see this
        await _hub.Clients.User(alert.UserId).SendAsync("AlertTriggered", notification, ct);
    }
}
