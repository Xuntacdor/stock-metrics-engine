using backend_api.Api.Data;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Workers;


public class RiskMonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RiskMonitorWorker> _logger;

    private const decimal CallMarginThreshold = 0.85m;
    private const decimal ForceSellThreshold  = 0.80m;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    public RiskMonitorWorker(IServiceScopeFactory scopeFactory, ILogger<RiskMonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RiskMonitorWorker started. Checking every {Interval}s. " +
            "Call Margin threshold: {CM}%. Force Sell threshold: {FS}%.",
            CheckInterval.TotalSeconds,
            CallMarginThreshold * 100,
            ForceSellThreshold * 100);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await MonitorAllUsersAsync(stoppingToken);
        }

        _logger.LogInformation("RiskMonitorWorker stopped.");
    }

    private async Task MonitorAllUsersAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context    = scope.ServiceProvider.GetRequiredService<QuantIQContext>();
        var riskService  = scope.ServiceProvider.GetRequiredService<Services.IMarginRiskService>();
        var alertRepo  = scope.ServiceProvider.GetRequiredService<IRiskAlertRepository>();

        var usersWithLoan = await context.CashWallets
            .Where(w => w.LoanAmount != null && w.LoanAmount > 0)
            .Select(w => w.UserId)
            .ToListAsync(stoppingToken);

        if (usersWithLoan.Count == 0) return;

        _logger.LogInformation("RiskMonitorWorker: Checking {Count} user(s) with loan balance.", usersWithLoan.Count);

        foreach (var userId in usersWithLoan)
        {
            if (stoppingToken.IsCancellationRequested) break;
            await CheckUserRiskAsync(userId, riskService, alertRepo, stoppingToken);
        }

        await alertRepo.SaveChangesAsync();
    }

    private async Task CheckUserRiskAsync(
        string userId,
        Services.IMarginRiskService riskService,
        IRiskAlertRepository alertRepo,
        CancellationToken stoppingToken)
    {
        try
        {
            var rtt = await riskService.CalculateRttAsync(userId);

            if (rtt == decimal.MaxValue) return; 

            _logger.LogInformation("RiskMonitorWorker: User {UserId} — Rtt = {Rtt:P2}", userId, rtt);

            if (rtt < ForceSellThreshold)
            {
                _logger.LogWarning(
                    "FORCE SELL: User {UserId} Rtt={Rtt:P2} < {Threshold:P0}.",
                    userId, rtt, ForceSellThreshold);

                await alertRepo.AddAsync(new RiskAlert
                {
                    UserId    = userId,
                    AlertType = "FORCE_SELL",
                    Rtt       = rtt,
                    Message   = $"Tỷ lệ tài khoản {rtt:P2} xuống dưới ngưỡng {ForceSellThreshold:P0}. Hệ thống đang bán giải chấp tự động.",
                    CreatedAt = DateTime.UtcNow
                });

                await riskService.ExecuteForceSellAsync(userId);
            }
            else if (rtt < CallMarginThreshold)
            {
                _logger.LogWarning(
                    "CALL MARGIN: User {UserId} Rtt={Rtt:P2} < {Threshold:P0}. Sending warning.",
                    userId, rtt, CallMarginThreshold);

                await alertRepo.AddAsync(new RiskAlert
                {
                    UserId    = userId,
                    AlertType = "CALL_MARGIN",
                    Rtt       = rtt,
                    Message   = $"Tỷ lệ tài khoản {rtt:P2} xuống dưới ngưỡng {CallMarginThreshold:P0}. Vui lòng nộp thêm tiền ký quỹ.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RiskMonitorWorker: Error checking risk for User {UserId}.", userId);
        }
    }
}
