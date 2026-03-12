using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class MarginRiskService : IMarginRiskService
{
    private readonly IWalletRepository _walletRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IMarginRatioRepository _marginRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly QuantIQContext _context;
    private readonly ILogger<MarginRiskService> _logger;

    public MarginRiskService(
        IWalletRepository walletRepo,
        IPortfolioRepository portfolioRepo,
        IMarginRatioRepository marginRepo,
        IOrderRepository orderRepo,
        ITransactionRepository transactionRepo,
        QuantIQContext context,
        ILogger<MarginRiskService> logger)
    {
        _walletRepo = walletRepo;
        _portfolioRepo = portfolioRepo;
        _marginRepo = marginRepo;
        _orderRepo = orderRepo;
        _transactionRepo = transactionRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<decimal> GetBuyingPowerAsync(string userId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Wallet not found.");

        var portfolios = (await _portfolioRepo.GetByUserIdAsync(userId)).ToList();

        decimal marginValue = 0m;

        foreach (var pos in portfolios.Where(p => (p.TotalQuantity ?? 0) > 0))
        {
            var latestCandle = await _context.Candles
                .Where(c => c.Symbol == pos.Symbol)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();

            var marketPrice = latestCandle?.Close ?? 0m;

            var ratio = await _marginRepo.GetActiveBySymbolAsync(pos.Symbol);
            var initialRate = ratio?.InitialRate ?? 0m;

            marginValue += marketPrice * (pos.TotalQuantity ?? 0) * (initialRate / 100m);
        }

        return (wallet.AvailableBalance ?? 0m) + marginValue;
    }

    
    public async Task<decimal> CalculateRttAsync(string userId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Wallet not found.");

        var loanAmount = wallet.LoanAmount ?? 0m;

        if (loanAmount <= 0m)
            return decimal.MaxValue;

        var totalAssets = wallet.Balance ?? 0m;
        var portfolios = (await _portfolioRepo.GetByUserIdAsync(userId)).ToList();

        foreach (var pos in portfolios.Where(p => (p.TotalQuantity ?? 0) > 0))
        {
            var latestCandle = await _context.Candles
                .Where(c => c.Symbol == pos.Symbol)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();

            var marketPrice = latestCandle?.Close ?? pos.AvgCostPrice ?? 0m;
            totalAssets += marketPrice * (pos.TotalQuantity ?? 0);
        }

        var netAssets = totalAssets - loanAmount;
        return netAssets / loanAmount;
    }

    public async Task<bool> ValidatePreTradeAsync(string userId, string symbol, int quantity, decimal price)
    {
        var totalCost = price * quantity;
        var buyingPower = await GetBuyingPowerAsync(userId);

        if (totalCost > buyingPower)
        {
            _logger.LogWarning(
                "Pre-Trade Risk FAILED for User {UserId}: Need {Need:N0}, BuyingPower {BP:N0}",
                userId, totalCost, buyingPower);
            return false;
        }

        return true;
    }

 
    public async Task ExecuteForceSellAsync(string userId)
    {
        _logger.LogWarning("=== FORCE SELL triggered for User {UserId} ===", userId);

        var wallet = await _walletRepo.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            _logger.LogWarning("Force Sell: Wallet not found for User {UserId}. Aborting.", userId);
            return;
        }

        var positions = (await _portfolioRepo.GetByUserIdAsync(userId))
            .Where(p => (p.AvailableQuantity ?? 0) > 0)
            .ToList();

        if (positions.Count == 0)
        {
            _logger.LogInformation("Force Sell: No available positions for User {UserId}.", userId);
            return;
        }

        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var pos in positions)
            {
                var latestCandle = await _context.Candles
                    .Where(c => c.Symbol == pos.Symbol)
                    .OrderByDescending(c => c.Timestamp)
                    .FirstOrDefaultAsync();

                var sellPrice = latestCandle?.Close ?? pos.AvgCostPrice ?? 0m;
                if (sellPrice <= 0m)
                {
                    _logger.LogWarning("Force Sell: Cannot determine sell price for {Symbol}. Skipping.", pos.Symbol);
                    continue;
                }

                var qty = pos.AvailableQuantity ?? 0;
                var proceeds = sellPrice * qty;

                var orderId = Guid.NewGuid().ToString();
                var order = new Order
                {
                    OrderId   = orderId,
                    UserId    = userId,
                    Symbol    = pos.Symbol,
                    Side      = "SELL",
                    OrderType = "FORCE_SELL",
                    RequestQty = qty,
                    Price     = sellPrice,
                    MatchedQty = qty,
                    AvgMatchedPrice = sellPrice,
                    Status    = "FILLED",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _orderRepo.AddAsync(order);
                await _orderRepo.SaveChangesAsync();

                var balanceBefore = wallet.Balance ?? 0m;
                wallet.Balance += proceeds;
                wallet.LastUpdated = DateTime.UtcNow;
                _walletRepo.Update(wallet);
                await _walletRepo.SaveChangesAsync();

                pos.TotalQuantity -= qty;
                pos.LockedQuantity = Math.Max(0, (pos.LockedQuantity ?? 0) - qty);
                if (pos.TotalQuantity <= 0)
                {
                    pos.TotalQuantity = 0;
                    pos.LockedQuantity = 0;
                    pos.AvgCostPrice = 0;
                }
                _portfolioRepo.Update(pos);
                await _portfolioRepo.SaveChangesAsync();

                var tx = new Transaction
                {
                    UserId      = userId,
                    RefId       = orderId,
                    TransType   = "FORCE_SELL",
                    Amount      = proceeds,
                    BalanceBefore = balanceBefore,
                    BalanceAfter  = wallet.Balance ?? 0m,
                    Description = $"[Force Sell] Bán {qty} CP {pos.Symbol} tại {sellPrice:N0} ₫/CP",
                    TransTime   = DateTime.UtcNow
                };
                await _transactionRepo.AddAsync(tx);
                await _transactionRepo.SaveChangesAsync();

                _logger.LogInformation(
                    "Force Sell: Sold {Qty} shares of {Symbol} at {Price:N0} for User {UserId}.",
                    qty, pos.Symbol, sellPrice, userId);
            }

            await dbTx.CommitAsync();
        }
        catch (Exception ex)
        {
            await dbTx.RollbackAsync();
            _logger.LogError(ex, "Force Sell: Transaction failed for User {UserId}.", userId);
            throw;
        }
    }
}
