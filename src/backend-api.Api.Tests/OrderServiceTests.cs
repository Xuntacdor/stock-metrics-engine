using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="OrderService"/>.
/// All repository operations are mocked. A fresh InMemory <see cref="QuantIQContext"/>
/// is created per test so that BeginTransactionAsync (a no-op in InMemory) does not
/// throw the "TransactionIgnoredWarning" error.
/// </summary>
public class OrderServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QuantIQContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<QuantIQContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new QuantIQContext(options);
    }

    private static CashWallet DefaultWallet(string userId = "user1") => new()
    {
        WalletId        = 1,
        UserId          = userId,
        Balance         = 10_000_000m,
        LockedAmount    = 0m,
        AvailableBalance = 10_000_000m,   // computed — set manually for tests
        LoanAmount      = 0m,
        RowVersion      = new byte[8]
    };

    private static Portfolio DefaultPortfolio(string userId = "user1", string symbol = "FPT",
        int totalQty = 100, int lockedQty = 0, decimal avgCost = 80_000m) => new()
    {
        UserId           = userId,
        Symbol           = symbol,
        TotalQuantity    = totalQty,
        LockedQuantity   = lockedQty,
        AvailableQuantity = totalQty - lockedQty,   // computed — set manually
        AvgCostPrice     = avgCost,
        RowVersion       = new byte[8]
    };

    private static Symbol DefaultSymbol(string symbolId = "FPT") => new()
    {
        Symbol1     = symbolId,
        CompanyName = "FPT Corporation"
    };

    /// <summary>
    /// Builds the service under test together with all the mock dependencies
    /// it needs. Returns both the service and the mocks so individual tests
    /// can configure additional expectations.
    /// </summary>
    private static (OrderService svc,
                    Mock<IOrderRepository>      orderRepo,
                    Mock<IPortfolioRepository>  portfolioRepo,
                    Mock<IWalletRepository>     walletRepo,
                    Mock<ITransactionRepository> transactionRepo,
                    Mock<ISymbolRepository>     symbolRepo,
                    Mock<IMarginRiskService>    riskService,
                    Mock<IAuditLogService>      auditLog,
                    QuantIQContext              context)
        BuildSut()
    {
        var orderRepo       = new Mock<IOrderRepository>();
        var portfolioRepo   = new Mock<IPortfolioRepository>();
        var walletRepo      = new Mock<IWalletRepository>();
        var transactionRepo = new Mock<ITransactionRepository>();
        var symbolRepo      = new Mock<ISymbolRepository>();
        var riskService     = new Mock<IMarginRiskService>();
        var auditLog        = new Mock<IAuditLogService>();
        var context         = BuildContext();

        // Common no-op SaveChanges
        orderRepo      .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        walletRepo     .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        portfolioRepo  .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        transactionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        orderRepo      .Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        portfolioRepo  .Setup(r => r.AddAsync(It.IsAny<Portfolio>())).Returns(Task.CompletedTask);
        transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

        auditLog.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var svc = new OrderService(
            orderRepo.Object,
            portfolioRepo.Object,
            walletRepo.Object,
            transactionRepo.Object,
            symbolRepo.Object,
            riskService.Object,
            context,
            auditLog.Object);

        return (svc, orderRepo, portfolioRepo, walletRepo, transactionRepo, symbolRepo, riskService, auditLog, context);
    }

    private static PlaceOrderRequest BuyRequest(string symbol = "FPT", int qty = 10, decimal price = 50_000m) =>
        new() { Symbol = symbol, Side = "BUY", OrderType = "LO", Quantity = qty, Price = price };

    private static PlaceOrderRequest SellRequest(string symbol = "FPT", int qty = 10, decimal price = 50_000m) =>
        new() { Symbol = symbol, Side = "SELL", OrderType = "LO", Quantity = qty, Price = price };

    // ── PlaceOrderAsync — BUY path ─────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Buy_SymbolNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, _, _, _, _, symbolRepo, _, _, _) = BuildSut();
        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync((Symbol?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.PlaceOrderAsync("user1", BuyRequest()));
    }

    [Fact]
    public async Task PlaceOrder_Buy_WalletNotFound_ThrowsInvalidOperationException()
    {
        var (svc, _, _, walletRepo, _, symbolRepo, _, _, _) = BuildSut();
        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync((CashWallet?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.PlaceOrderAsync("user1", BuyRequest()));
    }

    [Fact]
    public async Task PlaceOrder_Buy_RiskCheckFails_ThrowsInvalidOperationException()
    {
        var (svc, _, _, walletRepo, _, symbolRepo, riskService, _, _) = BuildSut();
        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        // Risk check returns false
        riskService
            .Setup(r => r.ValidatePreTradeAsync("user1", "FPT", 10, 50_000m))
            .ReturnsAsync(false);
        riskService
            .Setup(r => r.GetBuyingPowerAsync("user1"))
            .ReturnsAsync(100_000m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.PlaceOrderAsync("user1", BuyRequest()));
    }

    [Fact]
    public async Task PlaceOrder_Buy_NewPortfolio_CreatesPortfolioAndFillsOrder()
    {
        var (svc, orderRepo, portfolioRepo, walletRepo, transactionRepo, symbolRepo, riskService, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        var wallet = DefaultWallet();
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        riskService.Setup(r => r.ValidatePreTradeAsync("user1", "FPT", 10, 50_000m)).ReturnsAsync(true);

        // First call (HandleSellPreCheck path is BUY so we skip it)
        // SettleBuyAsync calls GetByUserAndSymbolAsync — returns null → new portfolio
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync((Portfolio?)null);

        var result = await svc.PlaceOrderAsync("user1", BuyRequest());

        Assert.Equal("FILLED", result.Status);
        Assert.Equal(10, result.MatchedQty);
        portfolioRepo.Verify(r => r.AddAsync(It.Is<Portfolio>(p =>
            p.UserId == "user1" &&
            p.Symbol == "FPT" &&
            p.TotalQuantity == 10 &&
            p.AvgCostPrice == 50_000m)), Times.Once);
    }

    [Fact]
    public async Task PlaceOrder_Buy_ExistingPortfolio_UpdatesAvgCostPriceCorrectly()
    {
        // Existing: 100 shares @ 80,000 — New buy: 50 @ 90,000
        // Expected avg = (80,000×100 + 90,000×50) / 150 = 83,333.33...
        var (svc, _, portfolioRepo, walletRepo, _, symbolRepo, riskService, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet(userId: "user1"));
        riskService.Setup(r => r.ValidatePreTradeAsync("user1", "FPT", 50, 90_000m)).ReturnsAsync(true);

        var existingPortfolio = DefaultPortfolio(totalQty: 100, avgCost: 80_000m);
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync(existingPortfolio);

        Portfolio? captured = null;
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>()))
            .Callback<Portfolio>(p => captured = p);

        await svc.PlaceOrderAsync("user1", BuyRequest(qty: 50, price: 90_000m));

        Assert.NotNull(captured);
        var expectedAvg = (80_000m * 100 + 90_000m * 50) / 150m;
        Assert.Equal(expectedAvg, captured!.AvgCostPrice);
        Assert.Equal(150, captured.TotalQuantity);
    }

    // ── PlaceOrderAsync — SELL path ────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_Sell_NoPortfolio_ThrowsInvalidOperationException()
    {
        var (svc, _, portfolioRepo, walletRepo, _, symbolRepo, _, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync((Portfolio?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.PlaceOrderAsync("user1", SellRequest()));
    }

    [Fact]
    public async Task PlaceOrder_Sell_InsufficientAvailableQty_ThrowsInvalidOperationException()
    {
        var (svc, _, portfolioRepo, walletRepo, _, symbolRepo, _, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        // AvailableQuantity = 5, but we request to sell 10
        var portfolio = DefaultPortfolio(totalQty: 5, lockedQty: 0);
        portfolio.AvailableQuantity = 5;
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync(portfolio);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.PlaceOrderAsync("user1", SellRequest(qty: 10)));
    }

    [Fact]
    public async Task PlaceOrder_Sell_FullPosition_ZerosAvgCostPrice()
    {
        // Sell all 100 shares → TotalQuantity = 0, AvgCostPrice = 0
        var (svc, _, portfolioRepo, walletRepo, _, symbolRepo, _, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        var portfolio = DefaultPortfolio(totalQty: 100, lockedQty: 0, avgCost: 80_000m);
        portfolio.AvailableQuantity = 100;

        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync(portfolio);

        Portfolio? captured = null;
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>()))
            .Callback<Portfolio>(p => captured = p);

        await svc.PlaceOrderAsync("user1", SellRequest(qty: 100, price: 90_000m));

        Assert.NotNull(captured);
        Assert.Equal(0, captured!.TotalQuantity);
        Assert.Equal(0m, captured.AvgCostPrice);
    }

    [Fact]
    public async Task PlaceOrder_Sell_PartialPosition_DecrementsTotalQuantity()
    {
        // Sell 30 out of 100 shares → TotalQuantity = 70
        var (svc, _, portfolioRepo, walletRepo, _, symbolRepo, _, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        var portfolio = DefaultPortfolio(totalQty: 100, lockedQty: 0, avgCost: 80_000m);
        portfolio.AvailableQuantity = 100;

        // HandleSellPreCheck + SettleSellAsync both call GetByUserAndSymbolAsync
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync(portfolio);

        Portfolio? captured = null;
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>()))
            .Callback<Portfolio>(p => captured = p);

        await svc.PlaceOrderAsync("user1", SellRequest(qty: 30, price: 90_000m));

        Assert.NotNull(captured);
        Assert.Equal(70, captured!.TotalQuantity);
        Assert.True(captured.AvgCostPrice > 0m);
    }

    [Fact]
    public async Task PlaceOrder_InvalidSide_ThrowsArgumentException()
    {
        var (svc, _, _, walletRepo, _, symbolRepo, _, _, _) = BuildSut();

        symbolRepo.Setup(r => r.GetByIdAsync("FPT")).ReturnsAsync(DefaultSymbol());
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        var request = new PlaceOrderRequest
        {
            Symbol    = "FPT",
            Side      = "HOLD",   // invalid
            OrderType = "LO",
            Quantity  = 10,
            Price     = 50_000m
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.PlaceOrderAsync("user1", request));
    }

    // ── CancelOrderAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CancelOrder_OrderNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, orderRepo, _, _, _, _, _, _, _) = BuildSut();
        orderRepo.Setup(r => r.GetByIdAsync("ord-1")).ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.CancelOrderAsync("user1", "ord-1"));
    }

    [Fact]
    public async Task CancelOrder_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var (svc, orderRepo, _, _, _, _, _, _, _) = BuildSut();
        var order = new Order
        {
            OrderId   = "ord-1",
            UserId    = "other-user",
            Symbol    = "FPT",
            Side      = "BUY",
            OrderType = "LO",
            Status    = "PENDING",
            RequestQty = 10,
            Price     = 50_000m,
            MatchedQty = 0
        };
        orderRepo.Setup(r => r.GetByIdAsync("ord-1")).ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => svc.CancelOrderAsync("user1", "ord-1"));
    }

    [Fact]
    public async Task CancelOrder_AlreadyFilled_ThrowsInvalidOperationException()
    {
        var (svc, orderRepo, _, walletRepo, _, _, _, _, _) = BuildSut();
        var order = new Order
        {
            OrderId    = "ord-1",
            UserId     = "user1",
            Symbol     = "FPT",
            Side       = "BUY",
            OrderType  = "LO",
            Status     = "FILLED",
            RequestQty = 10,
            Price      = 50_000m,
            MatchedQty = 10
        };
        orderRepo.Setup(r => r.GetByIdAsync("ord-1")).ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelOrderAsync("user1", "ord-1"));
    }

    [Fact]
    public async Task CancelOrder_AlreadyCancelled_ThrowsInvalidOperationException()
    {
        var (svc, orderRepo, _, _, _, _, _, _, _) = BuildSut();
        var order = new Order
        {
            OrderId    = "ord-1",
            UserId     = "user1",
            Symbol     = "FPT",
            Side       = "BUY",
            OrderType  = "LO",
            Status     = "CANCELLED",
            RequestQty = 10,
            Price      = 50_000m,
            MatchedQty = 0
        };
        orderRepo.Setup(r => r.GetByIdAsync("ord-1")).ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelOrderAsync("user1", "ord-1"));
    }

    [Fact]
    public async Task CancelOrder_BuyOrder_Pending_RefundsLockedAmount()
    {
        // PENDING BUY for 10 shares @ 50,000 → locked += 500,000 on place.
        // Cancel should subtract 500,000 from LockedAmount.
        var (svc, orderRepo, _, walletRepo, _, _, _, _, _) = BuildSut();

        var order = new Order
        {
            OrderId    = "ord-1",
            UserId     = "user1",
            Symbol     = "FPT",
            Side       = "BUY",
            OrderType  = "LO",
            Status     = "PENDING",
            RequestQty = 10,
            Price      = 50_000m,
            MatchedQty = 0
        };
        orderRepo.Setup(r => r.GetByIdAsync("ord-1")).ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var wallet = DefaultWallet();
        wallet.LockedAmount = 500_000m;
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);

        CashWallet? capturedWallet = null;
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>()))
            .Callback<CashWallet>(w => capturedWallet = w);

        await svc.CancelOrderAsync("user1", "ord-1");

        Assert.NotNull(capturedWallet);
        // refund = 50,000 × 10 = 500,000 → LockedAmount = 500,000 - 500,000 = 0
        Assert.Equal(0m, capturedWallet!.LockedAmount);
        Assert.Equal("CANCELLED", order.Status);
    }

    [Fact]
    public async Task CancelOrder_SellOrder_Pending_ReleasesLockedQuantity()
    {
        // PENDING SELL for 10 shares → those shares were locked on place.
        // Cancel should subtract 10 from LockedQuantity.
        var (svc, orderRepo, portfolioRepo, walletRepo, _, _, _, _, _) = BuildSut();

        var order = new Order
        {
            OrderId    = "ord-2",
            UserId     = "user1",
            Symbol     = "FPT",
            Side       = "SELL",
            OrderType  = "LO",
            Status     = "PENDING",
            RequestQty = 10,
            Price      = 90_000m,
            MatchedQty = 0
        };
        orderRepo.Setup(r => r.GetByIdAsync("ord-2")).ReturnsAsync(order);
        orderRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        var portfolio = DefaultPortfolio(totalQty: 100, lockedQty: 10);
        portfolioRepo
            .Setup(r => r.GetByUserAndSymbolAsync("user1", "FPT"))
            .ReturnsAsync(portfolio);

        Portfolio? capturedPortfolio = null;
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>()))
            .Callback<Portfolio>(p => capturedPortfolio = p);

        await svc.CancelOrderAsync("user1", "ord-2");

        Assert.NotNull(capturedPortfolio);
        // remainingQty = 10 - 0 = 10 → LockedQuantity = 10 - 10 = 0
        Assert.Equal(0, capturedPortfolio!.LockedQuantity);
        Assert.Equal("CANCELLED", order.Status);
    }

    // ── GetMyOrdersAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrders_ReturnsOrdersMappedToResponse()
    {
        var (svc, orderRepo, _, _, _, _, _, _, _) = BuildSut();

        var orders = new List<Order>
        {
            new()
            {
                OrderId    = "ord-a",
                UserId     = "user1",
                Symbol     = "FPT",
                Side       = "BUY",
                OrderType  = "LO",
                Status     = "FILLED",
                RequestQty = 5,
                Price      = 70_000m,
                MatchedQty = 5,
                AvgMatchedPrice = 70_000m,
                CreatedAt  = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new()
            {
                OrderId    = "ord-b",
                UserId     = "user1",
                Symbol     = "VNM",
                Side       = "SELL",
                OrderType  = "LO",
                Status     = "PENDING",
                RequestQty = 20,
                Price      = 45_000m,
                MatchedQty = 0,
                CreatedAt  = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };
        orderRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(orders);

        var result = (await svc.GetMyOrdersAsync("user1")).ToList();

        Assert.Equal(2, result.Count);

        var first = result[0];
        Assert.Equal("ord-a", first.OrderId);
        Assert.Equal("FPT",   first.Symbol);
        Assert.Equal("BUY",   first.Side);
        Assert.Equal("FILLED", first.Status);
        Assert.Equal(5, first.RequestQty);
        Assert.Equal(70_000m, first.Price);

        var second = result[1];
        Assert.Equal("ord-b", second.OrderId);
        Assert.Equal("PENDING", second.Status);
    }
}
