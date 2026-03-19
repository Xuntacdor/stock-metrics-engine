using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISymbolRepository _symbolRepo;
    private readonly IMarginRiskService _riskService;
    private readonly QuantIQContext _context;
    private readonly IAuditLogService _auditLog;

    public OrderService(
        IOrderRepository orderRepo,
        IPortfolioRepository portfolioRepo,
        IWalletRepository walletRepo,
        ITransactionRepository transactionRepo,
        ISymbolRepository symbolRepo,
        IMarginRiskService riskService,
        QuantIQContext context,
        IAuditLogService auditLog)
    {
        _orderRepo = orderRepo;
        _portfolioRepo = portfolioRepo;
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
        _symbolRepo = symbolRepo;
        _riskService = riskService;
        _context = context;
        _auditLog = auditLog;
    }

    public async Task<OrderResponse> PlaceOrderAsync(string userId, PlaceOrderRequest request)
    {
        var symbol = await _symbolRepo.GetByIdAsync(request.Symbol)
            ?? throw new KeyNotFoundException($"Symbol '{request.Symbol}' not found.");

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Wallet not found.");

        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            UserId = userId,
            Symbol = request.Symbol,
            Side = request.Side.ToUpper(),
            OrderType = request.OrderType.ToUpper(),
            RequestQty = request.Quantity,
            Price = request.Price,
            Status = "PENDING",
            MatchedQty = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (request.Side.ToUpper() == "BUY")
                await HandleBuyPreCheck(wallet, order);
            else if (request.Side.ToUpper() == "SELL")
                await HandleSellPreCheck(userId, order);
            else
                throw new ArgumentException("Side must be BUY or SELL.");

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            await MatchOrderAsync(order, wallet, userId);

            await dbTx.CommitAsync();
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }

        // Audit Trail
        await _auditLog.LogAsync(userId, "PlaceOrder", new
        {
            order.OrderId,
            order.Symbol,
            order.Side,
            order.OrderType,
            order.RequestQty,
            order.Price,
            order.Status
        });

        return MapToResponse(order);
    }

    public async Task<IEnumerable<OrderResponse>> GetMyOrdersAsync(string userId)
    {
        var orders = await _orderRepo.GetByUserIdAsync(userId);
        return orders.Select(MapToResponse);
    }

    public async Task CancelOrderAsync(string userId, string orderId)
    {
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException("Order not found.");

        if (order.UserId != userId)
            throw new UnauthorizedAccessException("You don't have permission to cancel this order.");

        if (order.Status == "FILLED" || order.Status == "CANCELLED")
            throw new InvalidOperationException($"Cannot cancel order in '{order.Status}' state.");

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new InvalidOperationException("Wallet not found.");

        await using var dbTx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (order.Side == "BUY")
            {
                var remainingQty = order.RequestQty - (order.MatchedQty ?? 0);
                var refundAmount = order.Price * remainingQty;
                wallet.LockedAmount -= refundAmount;
                wallet.LastUpdated = DateTime.UtcNow;
                _walletRepo.Update(wallet);
            }
            else if (order.Side == "SELL")
            {
                var portfolio = await _portfolioRepo.GetByUserAndSymbolAsync(userId, order.Symbol);
                if (portfolio != null)
                {
                    var remainingQty = order.RequestQty - (order.MatchedQty ?? 0);
                    portfolio.LockedQuantity -= remainingQty;
                    _portfolioRepo.Update(portfolio);
                }
            }

            order.Status = "CANCELLED";
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepo.Update(order);

            await _orderRepo.SaveChangesAsync();
            await dbTx.CommitAsync();
        }
        catch
        {
            await dbTx.RollbackAsync();
            throw;
        }

        // Audit Trail
        await _auditLog.LogAsync(userId, "CancelOrder", new { orderId, order.Symbol, order.Side });
    }

    private async Task HandleBuyPreCheck(CashWallet wallet, Order order)
    {
        var isAllowed = await _riskService.ValidatePreTradeAsync(
            order.UserId, order.Symbol, order.RequestQty, order.Price);

        if (!isAllowed)
        {
            var buyingPower = await _riskService.GetBuyingPowerAsync(order.UserId);
            var totalCost = order.Price * order.RequestQty;
            throw new InvalidOperationException(
                $"Pre-trade risk check failed. Need: {totalCost:N0} VNĐ, Buying Power: {buyingPower:N0} VNĐ.");
        }

        var cost = order.Price * order.RequestQty;
        wallet.LockedAmount += cost;
        wallet.LastUpdated = DateTime.UtcNow;
        _walletRepo.Update(wallet);
        await _walletRepo.SaveChangesAsync();
    }

    private async Task HandleSellPreCheck(string userId, Order order)
    {
        var portfolio = await _portfolioRepo.GetByUserAndSymbolAsync(userId, order.Symbol)
            ?? throw new InvalidOperationException($"You don't own symbol {order.Symbol}.");

        if (portfolio.AvailableQuantity < order.RequestQty)
            throw new InvalidOperationException(
                $"Insufficient quantity. Need: {order.RequestQty}, Available: {portfolio.AvailableQuantity}.");

        portfolio.LockedQuantity += order.RequestQty;
        _portfolioRepo.Update(portfolio);
        await _portfolioRepo.SaveChangesAsync();
    }

    private async Task MatchOrderAsync(Order order, CashWallet wallet, string userId)
    {
        var matchedQty = order.RequestQty;
        var matchPrice = order.Price;

        order.MatchedQty = matchedQty;
        order.AvgMatchedPrice = matchPrice;
        order.Status = "FILLED";
        order.UpdatedAt = DateTime.UtcNow;

        if (order.Side == "BUY")
            await SettleBuyAsync(order, wallet, userId, matchedQty, matchPrice);
        else
            await SettleSellAsync(order, wallet, userId, matchedQty, matchPrice);

        _orderRepo.Update(order);
        await _orderRepo.SaveChangesAsync();
    }

    private async Task SettleBuyAsync(Order order, CashWallet wallet, string userId, int matchedQty, decimal matchPrice)
    {
        var cost = matchPrice * matchedQty;

        var balanceBefore = wallet.Balance ?? 0;
        wallet.Balance -= cost;
        wallet.LockedAmount -= cost;
        wallet.LastUpdated = DateTime.UtcNow;
        _walletRepo.Update(wallet);
        await _walletRepo.SaveChangesAsync();

        var portfolio = await _portfolioRepo.GetByUserAndSymbolAsync(userId, order.Symbol);
        if (portfolio == null)
        {
            portfolio = new Portfolio
            {
                UserId = userId,
                Symbol = order.Symbol,
                TotalQuantity = matchedQty,
                LockedQuantity = 0,
                AvgCostPrice = matchPrice
            };
            await _portfolioRepo.AddAsync(portfolio);
        }
        else
        {
            var oldQty = portfolio.TotalQuantity ?? 0;
            var oldAvg = portfolio.AvgCostPrice ?? 0;
            portfolio.AvgCostPrice = (oldAvg * oldQty + matchPrice * matchedQty) / (oldQty + matchedQty);
            portfolio.TotalQuantity = oldQty + matchedQty;
            _portfolioRepo.Update(portfolio);
        }
        await _portfolioRepo.SaveChangesAsync();

        await RecordTransactionAsync(userId, order.OrderId, "BUY", -cost, balanceBefore, wallet.Balance ?? 0,
            $"Buy {matchedQty} shares of {order.Symbol} at {matchPrice:N0}");
    }

    private async Task SettleSellAsync(Order order, CashWallet wallet, string userId, int matchedQty, decimal matchPrice)
    {
        var proceeds = matchPrice * matchedQty;

        var balanceBefore = wallet.Balance ?? 0;
        wallet.Balance += proceeds;
        wallet.LastUpdated = DateTime.UtcNow;
        _walletRepo.Update(wallet);
        await _walletRepo.SaveChangesAsync();

        var portfolio = await _portfolioRepo.GetByUserAndSymbolAsync(userId, order.Symbol)
            ?? throw new InvalidOperationException("Portfolio not found when settle sell.");

        portfolio.TotalQuantity -= matchedQty;
        portfolio.LockedQuantity -= matchedQty;
        if (portfolio.TotalQuantity <= 0)
        {
            portfolio.TotalQuantity = 0;
            portfolio.LockedQuantity = 0;
            portfolio.AvgCostPrice = 0;
        }
        _portfolioRepo.Update(portfolio);
        await _portfolioRepo.SaveChangesAsync();

        await RecordTransactionAsync(userId, order.OrderId, "SELL", proceeds, balanceBefore, wallet.Balance ?? 0,
            $"Sell {matchedQty} shares of {order.Symbol} at {matchPrice:N0}");
    }

    private async Task RecordTransactionAsync(
        string userId, string refId, string transType,
        decimal amount, decimal balanceBefore, decimal balanceAfter, string description)
    {
        var tx = new Transaction
        {
            UserId = userId,
            RefId = refId,
            TransType = transType,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Description = description,
            TransTime = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(tx);
        await _transactionRepo.SaveChangesAsync();
    }

    private static OrderResponse MapToResponse(Order o) => new()
    {
        OrderId = o.OrderId,
        Symbol = o.Symbol,
        Side = o.Side,
        OrderType = o.OrderType,
        Status = o.Status ?? "PENDING",
        RequestQty = o.RequestQty,
        MatchedQty = o.MatchedQty,
        Price = o.Price,
        AvgMatchedPrice = o.AvgMatchedPrice,
        CreatedAt = o.CreatedAt
    };
}
