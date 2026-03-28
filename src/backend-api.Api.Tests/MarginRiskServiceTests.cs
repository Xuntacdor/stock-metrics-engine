using backend_api.Api.Data;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="MarginRiskService"/>.
///
/// MarginRiskService queries <see cref="QuantIQContext.Candles"/> directly via LINQ,
/// so the InMemory database is seeded with Candle rows as needed. All other data
/// access (wallet, portfolio, margin ratio, order, transaction) goes through mocked
/// repositories.
/// </summary>
public class MarginRiskServiceTests
{
    // ── Context factory ───────────────────────────────────────────────────────

    private static QuantIQContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<QuantIQContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new QuantIQContext(options);
    }

    // ── Default object builders ───────────────────────────────────────────────

    private static CashWallet DefaultWallet(
        string userId          = "user1",
        decimal balance        = 5_000_000m,
        decimal lockedAmount   = 0m,
        decimal availableBalance = 5_000_000m,
        decimal loanAmount     = 0m) => new()
    {
        WalletId         = 1,
        UserId           = userId,
        Balance          = balance,
        LockedAmount     = lockedAmount,
        AvailableBalance = availableBalance,   // computed column — set manually
        LoanAmount       = loanAmount,
        RowVersion       = new byte[8]
    };

    private static Portfolio DefaultPortfolio(
        string userId   = "user1",
        string symbol   = "FPT",
        int    totalQty = 100,
        int    lockedQty = 0,
        decimal avgCost = 80_000m) => new()
    {
        UserId            = userId,
        Symbol            = symbol,
        TotalQuantity     = totalQty,
        LockedQuantity    = lockedQty,
        AvailableQuantity = totalQty - lockedQty,   // computed — set manually
        AvgCostPrice      = avgCost,
        RowVersion        = new byte[8]
    };

    private static Candle MakeCandle(string symbol, long timestamp, decimal close) => new()
    {
        Symbol    = symbol,
        Timestamp = timestamp,
        Close     = close,
        Open      = close,
        High      = close,
        Low       = close,
        Volume    = 1000L
    };

    /// <summary>
    /// Builds the service under test together with all mock dependencies.
    /// Each call receives a shared <paramref name="context"/> so the caller
    /// can seed Candle data before invoking service methods.
    /// </summary>
    private static (MarginRiskService svc,
                    Mock<IWalletRepository>      walletRepo,
                    Mock<IPortfolioRepository>   portfolioRepo,
                    Mock<IMarginRatioRepository> marginRepo,
                    Mock<IOrderRepository>       orderRepo,
                    Mock<ITransactionRepository> transactionRepo)
        BuildSut(QuantIQContext context)
    {
        var walletRepo      = new Mock<IWalletRepository>();
        var portfolioRepo   = new Mock<IPortfolioRepository>();
        var marginRepo      = new Mock<IMarginRatioRepository>();
        var orderRepo       = new Mock<IOrderRepository>();
        var transactionRepo = new Mock<ITransactionRepository>();
        var logger          = NullLogger<MarginRiskService>.Instance;

        // Common no-op saves
        orderRepo      .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        walletRepo     .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        portfolioRepo  .Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        transactionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        orderRepo      .Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        transactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);

        var svc = new MarginRiskService(
            walletRepo.Object,
            portfolioRepo.Object,
            marginRepo.Object,
            orderRepo.Object,
            transactionRepo.Object,
            context,
            logger);

        return (svc, walletRepo, portfolioRepo, marginRepo, orderRepo, transactionRepo);
    }

    // ── GetBuyingPowerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBuyingPower_WalletNotFound_ThrowsInvalidOperationException()
    {
        var ctx = BuildContext();
        var (svc, walletRepo, _, _, _, _) = BuildSut(ctx);
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync((CashWallet?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.GetBuyingPowerAsync("user1"));
    }

    [Fact]
    public async Task GetBuyingPower_EmptyPortfolio_ReturnsAvailableCash()
    {
        // No positions → buying power = AvailableBalance only
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 3_000_000m));
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(Enumerable.Empty<Portfolio>());

        var result = await svc.GetBuyingPowerAsync("user1");

        Assert.Equal(3_000_000m, result);
    }

    [Fact]
    public async Task GetBuyingPower_PortfolioWithCandleAndMarginRatio_AddsMarginValue()
    {
        // Position: 100 shares FPT, latest close = 90,000, InitialRate = 50 %
        // marginValue = 90,000 × 100 × 0.50 = 4,500,000
        // totalBuyingPower = 5,000,000 + 4,500,000 = 9,500,000
        var ctx = BuildContext();
        ctx.Candles.Add(MakeCandle("FPT", 1_000L, 90_000m));
        await ctx.SaveChangesAsync();

        var (svc, walletRepo, portfolioRepo, marginRepo, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 5_000_000m));

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { DefaultPortfolio(totalQty: 100) });

        marginRepo.Setup(r => r.GetActiveBySymbolAsync("FPT"))
            .ReturnsAsync(new MarginRatio
            {
                RatioId         = 1,
                Symbol          = "FPT",
                InitialRate     = 50m,
                MaintenanceRate = 30m,
                EffectiveDate   = DateTime.UtcNow.AddDays(-10)
            });

        var result = await svc.GetBuyingPowerAsync("user1");

        Assert.Equal(9_500_000m, result);
    }

    [Fact]
    public async Task GetBuyingPower_PortfolioNoCandle_SkipsMarginContribution()
    {
        // No candle for symbol → marketPrice = 0 → margin contribution = 0
        // buying power = AvailableBalance only
        var ctx = BuildContext();   // no candles seeded
        var (svc, walletRepo, portfolioRepo, marginRepo, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 2_000_000m));

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { DefaultPortfolio(symbol: "VNM", totalQty: 50) });

        marginRepo.Setup(r => r.GetActiveBySymbolAsync("VNM"))
            .ReturnsAsync(new MarginRatio
            {
                RatioId         = 2,
                Symbol          = "VNM",
                InitialRate     = 40m,
                MaintenanceRate = 25m,
                EffectiveDate   = DateTime.UtcNow.AddDays(-5)
            });

        var result = await svc.GetBuyingPowerAsync("user1");

        Assert.Equal(2_000_000m, result);
    }

    [Fact]
    public async Task GetBuyingPower_PortfolioNoMarginRatio_SkipsMarginContribution()
    {
        // Candle exists but no MarginRatio → initialRate = 0 → contribution = 0
        var ctx = BuildContext();
        ctx.Candles.Add(MakeCandle("HPG", 2_000L, 25_000m));
        await ctx.SaveChangesAsync();

        var (svc, walletRepo, portfolioRepo, marginRepo, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 1_500_000m));

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { DefaultPortfolio(symbol: "HPG", totalQty: 200) });

        marginRepo.Setup(r => r.GetActiveBySymbolAsync("HPG"))
            .ReturnsAsync((MarginRatio?)null);

        var result = await svc.GetBuyingPowerAsync("user1");

        Assert.Equal(1_500_000m, result);
    }

    // ── CalculateRttAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateRtt_WalletNotFound_ThrowsInvalidOperationException()
    {
        var ctx = BuildContext();
        var (svc, walletRepo, _, _, _, _) = BuildSut(ctx);
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync((CashWallet?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CalculateRttAsync("user1"));
    }

    [Fact]
    public async Task CalculateRtt_ZeroLoan_ReturnsDecimalMaxValue()
    {
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(loanAmount: 0m));
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(Enumerable.Empty<Portfolio>());

        var result = await svc.CalculateRttAsync("user1");

        Assert.Equal(decimal.MaxValue, result);
    }

    [Fact]
    public async Task CalculateRtt_WithLoanAndPortfolioCandle_ComputesCorrectRtt()
    {
        // wallet.Balance = 2,000,000; loanAmount = 1,000,000
        // Position: 10 shares FPT, latest close = 100,000
        // totalAssets = 2,000,000 + (100,000 × 10) = 3,000,000
        // netAssets = 3,000,000 - 1,000,000 = 2,000,000
        // RTT = 2,000,000 / 1,000,000 = 2.0
        var ctx = BuildContext();
        ctx.Candles.Add(MakeCandle("FPT", 5_000L, 100_000m));
        await ctx.SaveChangesAsync();

        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(balance: 2_000_000m, loanAmount: 1_000_000m));

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { DefaultPortfolio(totalQty: 10, avgCost: 80_000m) });

        var result = await svc.CalculateRttAsync("user1");

        Assert.Equal(2.0m, result);
    }

    [Fact]
    public async Task CalculateRtt_WithLoanNoCandleFallsBackToAvgCost()
    {
        // No candle → marketPrice falls back to AvgCostPrice = 90,000
        // wallet.Balance = 1,000,000; loanAmount = 500,000
        // totalAssets = 1,000,000 + (90,000 × 5) = 1,450,000
        // netAssets = 1,450,000 - 500,000 = 950,000
        // RTT = 950,000 / 500,000 = 1.9
        var ctx = BuildContext();   // no candles
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(balance: 1_000_000m, loanAmount: 500_000m));

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { DefaultPortfolio(symbol: "VNM", totalQty: 5, avgCost: 90_000m) });

        var result = await svc.CalculateRttAsync("user1");

        Assert.Equal(1.9m, result);
    }

    // ── ValidatePreTradeAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidatePreTrade_CostLessThanBuyingPower_ReturnsTrue()
    {
        // AvailableBalance = 10,000,000; trade cost = 1,000 × 5,000 = 5,000,000 < 10,000,000
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 10_000_000m));
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(Enumerable.Empty<Portfolio>());

        var result = await svc.ValidatePreTradeAsync("user1", "FPT", 1_000, 5_000m);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidatePreTrade_CostExceedsBuyingPower_ReturnsFalse()
    {
        // AvailableBalance = 1,000,000; trade cost = 1,000 × 5,000 = 5,000,000 > 1,000,000
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 1_000_000m));
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(Enumerable.Empty<Portfolio>());

        var result = await svc.ValidatePreTradeAsync("user1", "FPT", 1_000, 5_000m);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidatePreTrade_CostEqualsBuyingPower_ReturnsTrue()
    {
        // AvailableBalance = 5,000,000; trade cost = 100 × 50,000 = 5,000,000 — exactly equal
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, _, _) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(DefaultWallet(availableBalance: 5_000_000m));
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(Enumerable.Empty<Portfolio>());

        var result = await svc.ValidatePreTradeAsync("user1", "FPT", 100, 50_000m);

        Assert.True(result);
    }

    // ── ExecuteForceSellAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteForceSell_WalletNotFound_ExitsWithoutAction()
    {
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, orderRepo, transactionRepo) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync((CashWallet?)null);

        // No exception, and no repo mutations
        await svc.ExecuteForceSellAsync("user1");

        orderRepo      .Verify(r => r.AddAsync(It.IsAny<Order>()),       Times.Never);
        transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteForceSell_NoAvailablePositions_ExitsWithoutAction()
    {
        var ctx = BuildContext();
        var (svc, walletRepo, portfolioRepo, _, orderRepo, transactionRepo) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        // All positions have AvailableQuantity = 0
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[]
            {
                new Portfolio
                {
                    UserId            = "user1",
                    Symbol            = "FPT",
                    TotalQuantity     = 100,
                    LockedQuantity    = 100,
                    AvailableQuantity = 0,
                    AvgCostPrice      = 80_000m,
                    RowVersion        = new byte[8]
                }
            });

        await svc.ExecuteForceSellAsync("user1");

        orderRepo      .Verify(r => r.AddAsync(It.IsAny<Order>()),       Times.Never);
        transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteForceSell_WithPositionAndCandle_CreatesOrderAndUpdatesWallet()
    {
        // Position: 50 available shares FPT, latest close = 95,000
        // proceeds = 95,000 × 50 = 4,750,000
        var ctx = BuildContext();
        ctx.Candles.Add(MakeCandle("FPT", 9_000L, 95_000m));
        await ctx.SaveChangesAsync();

        var (svc, walletRepo, portfolioRepo, _, orderRepo, transactionRepo) = BuildSut(ctx);

        var wallet = DefaultWallet(balance: 2_000_000m);
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);

        var position = new Portfolio
        {
            UserId            = "user1",
            Symbol            = "FPT",
            TotalQuantity     = 50,
            LockedQuantity    = 0,
            AvailableQuantity = 50,
            AvgCostPrice      = 80_000m,
            RowVersion        = new byte[8]
        };
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { position });

        CashWallet? capturedWallet = null;
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>()))
            .Callback<CashWallet>(w => capturedWallet = w);

        await svc.ExecuteForceSellAsync("user1");

        // Order created with FORCE_SELL type
        orderRepo.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.UserId    == "user1" &&
            o.Symbol    == "FPT"  &&
            o.Side      == "SELL" &&
            o.OrderType == "FORCE_SELL" &&
            o.RequestQty == 50 &&
            o.Price     == 95_000m &&
            o.Status    == "FILLED")), Times.Once);

        // Transaction recorded
        transactionRepo.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.UserId    == "user1" &&
            t.TransType == "FORCE_SELL" &&
            t.Amount    == 4_750_000m)), Times.Once);

        // Wallet balance updated
        Assert.NotNull(capturedWallet);
        Assert.Equal(2_000_000m + 4_750_000m, capturedWallet!.Balance);
    }

    [Fact]
    public async Task ExecuteForceSell_PositionNoCandleZeroAvgCost_SkipsPosition()
    {
        // No candle and AvgCostPrice = 0 → sellPrice = 0 → position skipped
        var ctx = BuildContext();   // no candles
        var (svc, walletRepo, portfolioRepo, _, orderRepo, transactionRepo) = BuildSut(ctx);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(DefaultWallet());

        var position = new Portfolio
        {
            UserId            = "user1",
            Symbol            = "XYZ",
            TotalQuantity     = 100,
            LockedQuantity    = 0,
            AvailableQuantity = 100,
            AvgCostPrice      = 0m,   // zero — no fallback price
            RowVersion        = new byte[8]
        };
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
            .ReturnsAsync(new[] { position });

        await svc.ExecuteForceSellAsync("user1");

        // sellPrice = 0 → logged and skipped → no order or transaction created
        orderRepo      .Verify(r => r.AddAsync(It.IsAny<Order>()),       Times.Never);
        transactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }
}
