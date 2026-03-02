namespace backend_api.Api.Workers;


public class DividendPayoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DividendPayoutWorker> _logger;

    private readonly TimeOnly _runAt = new(0, 5, 0);

    public DividendPayoutWorker(IServiceScopeFactory scopeFactory, ILogger<DividendPayoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DividendPayoutWorker started. Scheduled daily at {RunAt}", _runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var todayRunTime = now.Date.Add(_runAt.ToTimeSpan());

            if (now > todayRunTime)
                todayRunTime = todayRunTime.AddDays(1);

            var delay = todayRunTime - now;
            _logger.LogInformation("DividendPayoutWorker: next run in {Delay:hh\\:mm\\:ss} (at {RunTime:yyyy-MM-dd HH:mm})",
                delay, todayRunTime);

            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RunPayoutJobAsync(stoppingToken);
        }

        _logger.LogInformation("DividendPayoutWorker stopped.");
    }

    private async Task RunPayoutJobAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== DividendPayoutWorker: Running payout job at {Now} ===", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var actionRepo = scope.ServiceProvider.GetRequiredService<Services.ICorporateActionService>();
        var corporateActionRepo = scope.ServiceProvider.GetRequiredService<Repositories.ICorporateActionRepository>();

        try
        {
            var today = DateTime.UtcNow;
            var pendingActions = await corporateActionRepo.GetPendingForTodayAsync(today);
            var actionList = pendingActions.ToList();

            if (actionList.Count == 0)
            {
                _logger.LogInformation("DividendPayoutWorker: No pending actions for today ({Date:yyyy-MM-dd}).", today.Date);
                return;
            }

            _logger.LogInformation("DividendPayoutWorker: Found {Count} action(s) to process.", actionList.Count);

            foreach (var action in actionList)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    _logger.LogInformation("Processing action #{ActionId}: {Symbol} {ActionType}", 
                        action.ActionId, action.Symbol, action.ActionType);

                    await actionRepo.ProcessActionAsync(action.ActionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DividendPayoutWorker: Error processing action #{ActionId}", action.ActionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DividendPayoutWorker: Unexpected error during payout job.");
        }

        _logger.LogInformation("=== DividendPayoutWorker: Payout job completed. ===");
    }
}
