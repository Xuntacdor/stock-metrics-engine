using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class CorporateActionService : ICorporateActionService
{
    private readonly ICorporateActionRepository _actionRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ILogger<CorporateActionService> _logger;

    public CorporateActionService(
        ICorporateActionRepository actionRepo,
        IPortfolioRepository portfolioRepo,
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        ILogger<CorporateActionService> logger)
    {
        _actionRepo = actionRepo;
        _portfolioRepo = portfolioRepo;
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _logger = logger;
    }


    public async Task<IEnumerable<CorporateActionResponse>> GetAllAsync()
    {
        var actions = await _actionRepo.GetAllAsync();
        return actions.Select(MapToResponse);
    }

    public async Task<IEnumerable<CorporateActionResponse>> GetBySymbolAsync(string symbol)
    {
        var actions = await _actionRepo.GetBySymbolAsync(symbol.ToUpper());
        return actions.Select(MapToResponse);
    }

    public async Task<CorporateActionResponse> GetByIdAsync(int actionId)
    {
        var action = await _actionRepo.GetByIdAsync(actionId)
            ?? throw new KeyNotFoundException($"Corporate action #{actionId} not found.");
        return MapToResponse(action);
    }

    public async Task<CorporateActionResponse> CreateAsync(CreateCorporateActionRequest request)
    {
        ValidateActionType(request.ActionType);

        if (request.Ratio <= 0)
            throw new ArgumentException("Ratio must be greater than 0.");

        if (request.PaymentDate < request.RecordDate)
            throw new ArgumentException("PaymentDate must be on or after RecordDate.");

        var action = new CorporateAction
        {
            Symbol = request.Symbol.ToUpper(),
            ActionType = request.ActionType.ToUpper(),
            RecordDate = request.RecordDate.Date,
            PaymentDate = request.PaymentDate.Date,
            Ratio = request.Ratio,
            Status = "PENDING",
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _actionRepo.AddAsync(action);
        await _actionRepo.SaveChangesAsync();

        _logger.LogInformation("Created CorporateAction #{ActionId}: {Symbol} {ActionType} PaymentDate={PaymentDate}",
            action.ActionId, action.Symbol, action.ActionType, action.PaymentDate);

        return MapToResponse(action);
    }

    public async Task<CorporateActionResponse> UpdateAsync(int actionId, UpdateCorporateActionRequest request)
    {
        var action = await _actionRepo.GetByIdAsync(actionId)
            ?? throw new KeyNotFoundException($"Corporate action #{actionId} not found.");

        if (action.Status == "PROCESSED")
            throw new InvalidOperationException("Cannot update an already processed corporate action.");

        if (request.Status != null)
        {
            var allowed = new[] { "PENDING", "CANCELLED" };
            if (!allowed.Contains(request.Status.ToUpper()))
                throw new ArgumentException($"Status must be one of: {string.Join(", ", allowed)}");
            action.Status = request.Status.ToUpper();
        }

        if (request.RecordDate.HasValue) action.RecordDate = request.RecordDate.Value.Date;
        if (request.PaymentDate.HasValue) action.PaymentDate = request.PaymentDate.Value.Date;
        if (request.Ratio.HasValue)
        {
            if (request.Ratio <= 0) throw new ArgumentException("Ratio must be greater than 0.");
            action.Ratio = request.Ratio.Value;
        }
        if (request.Note != null) action.Note = request.Note;

        _actionRepo.Update(action);
        await _actionRepo.SaveChangesAsync();

        return MapToResponse(action);
    }


    public async Task ProcessActionAsync(int actionId)
    {
        var action = await _actionRepo.GetByIdAsync(actionId)
            ?? throw new KeyNotFoundException($"Corporate action #{actionId} not found.");

        if (action.Status != "PENDING")
        {
            _logger.LogWarning("Skipping action #{ActionId}: already {Status}", actionId, action.Status);
            return;
        }

        _logger.LogInformation("Processing CorporateAction #{ActionId}: {Symbol} {ActionType} Ratio={Ratio}",
            actionId, action.Symbol, action.ActionType, action.Ratio);

        var holdings = await _portfolioRepo.GetBySymbolAsync(action.Symbol);
        var eligibleHoldings = holdings.Where(p => (p.TotalQuantity ?? 0) > 0).ToList();

        _logger.LogInformation("Found {Count} eligible holders for {Symbol}", eligibleHoldings.Count, action.Symbol);

        int successCount = 0;
        int failCount = 0;

        foreach (var holding in eligibleHoldings)
        {
            try
            {
                await ProcessHolderAsync(action, holding);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing action #{ActionId} for user {UserId}", actionId, holding.UserId);
                failCount++;
            }
        }

        action.Status = "PROCESSED";
        action.ProcessedAt = DateTime.UtcNow;
        action.Note = (action.Note ?? "") +
                      $" | Processed: {successCount} OK, {failCount} FAILED at {DateTime.UtcNow:yyyy-MM-dd HH:mm}";

        _actionRepo.Update(action);
        await _actionRepo.SaveChangesAsync();

        _logger.LogInformation("Completed CorporateAction #{ActionId}: {SuccessCount} OK / {FailCount} FAILED",
            actionId, successCount, failCount);
    }

    private async Task ProcessHolderAsync(CorporateAction action, Portfolio holding)
    {
        var quantity = holding.TotalQuantity ?? 0;

        switch (action.ActionType)
        {            
            case "CASH_DIVIDEND":
            {
                var dividendAmount = action.Ratio * quantity;

                var wallet = await _walletRepo.GetByUserIdAsync(holding.UserId);
                if (wallet == null)
                {
                    _logger.LogWarning("No wallet for user {UserId}, skipping CASH_DIVIDEND", holding.UserId);
                    return;
                }

                var balanceBefore = wallet.Balance ?? 0;
                wallet.Balance = balanceBefore + dividendAmount;
                wallet.LastUpdated = DateTime.UtcNow;
                _walletRepo.Update(wallet);

                await _transactionRepo.AddAsync(new Transaction
                {
                    UserId = holding.UserId,
                    RefId = $"CA-{action.ActionId}-{holding.UserId}",
                    TransType = "CASH_DIVIDEND",
                    Amount = dividendAmount,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = wallet.Balance ?? 0,
                    Description = $"Cổ tức tiền mặt {action.Symbol}: {action.Ratio:N0} đ/cp × {quantity:N0} cp = {dividendAmount:N0} đ",
                    TransTime = DateTime.UtcNow
                });

                await _transactionRepo.SaveChangesAsync();
                await _walletRepo.SaveChangesAsync();

                _logger.LogInformation("CASH_DIVIDEND → User {UserId} received {Amount:N0} VND for {Symbol}",
                    holding.UserId, dividendAmount, action.Symbol);
                break;
            }

            case "STOCK_DIVIDEND":
            case "BONUS_SHARE":
            {
                var bonusShares = (int)Math.Floor(quantity * (double)action.Ratio);
                if (bonusShares <= 0)
                {
                    _logger.LogInformation("User {UserId} holds {Qty} cp × ratio {Ratio} → 0 bonus shares, skip",
                        holding.UserId, quantity, action.Ratio);
                    return;
                }

                var oldCost = (holding.TotalQuantity ?? 0) * (holding.AvgCostPrice ?? 0);
                var newTotalQty = (holding.TotalQuantity ?? 0) + bonusShares;
                holding.TotalQuantity = newTotalQty;
                holding.AvgCostPrice = newTotalQty > 0 ? oldCost / newTotalQty : 0;

                _portfolioRepo.Update(holding);
                await _portfolioRepo.SaveChangesAsync();

                await _transactionRepo.AddAsync(new Transaction
                {
                    UserId = holding.UserId,
                    RefId = $"CA-{action.ActionId}-{holding.UserId}",
                    TransType = action.ActionType,
                    Amount = 0,
                    BalanceBefore = 0,
                    BalanceAfter = 0,
                    Description = $"Cổ phiếu thưởng {action.Symbol}: {action.Ratio:P0} × {quantity:N0} cp = +{bonusShares:N0} cp",
                    TransTime = DateTime.UtcNow
                });
                await _transactionRepo.SaveChangesAsync();

                _logger.LogInformation("{ActionType} → User {UserId} received +{BonusShares} {Symbol}",
                    action.ActionType, holding.UserId, bonusShares, action.Symbol);
                break;
            }

            default:
                _logger.LogWarning("Unknown ActionType {ActionType} for action #{ActionId}", action.ActionType, action.ActionId);
                break;
        }
    }

    private static void ValidateActionType(string actionType)
    {
        var valid = new[] { "CASH_DIVIDEND", "STOCK_DIVIDEND", "BONUS_SHARE" };
        if (!valid.Contains(actionType.ToUpper()))
            throw new ArgumentException($"ActionType must be one of: {string.Join(", ", valid)}");
    }

    private static CorporateActionResponse MapToResponse(CorporateAction a) => new()
    {
        ActionId = a.ActionId,
        Symbol = a.Symbol,
        ActionType = a.ActionType,
        RecordDate = a.RecordDate,
        PaymentDate = a.PaymentDate,
        Ratio = a.Ratio,
        Status = a.Status,
        ProcessedAt = a.ProcessedAt,
        Note = a.Note,
        CreatedAt = a.CreatedAt
    };
}
