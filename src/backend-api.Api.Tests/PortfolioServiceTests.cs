using backend_api.Api.Data;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="PortfolioService"/>.
/// <see cref="QuantIQContext"/> is backed by InMemory EF Core for Candle queries;
/// <see cref="IPortfolioRepository"/> is mocked.
/// Each test gets an isolated database name (Guid) to prevent data leakage.
/// </summary>
public class PortfolioServiceTests
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

    /// <summary>
    /// Seeds a Symbol and N candles for that symbol.
    /// Candle timestamps are Unix-millis starting at base and incrementing by 1 per item.
    /// Closes are provided in chronological order; the last one is the "latest" price.
    /// </summary>
    private static void SeedSymbolAndCandles(
        QuantIQContext ctx, string symbol, decimal[] closes, long? volume = 10_000L)
    {
        ctx.Symbols.Add(new Symbol { Symbol1 = symbol, CompanyName = symbol + " Corp" });

        for (int i = 0; i < closes.Length; i++)
        {
            ctx.Candles.Add(new Candle
            {
                Symbol    = symbol,
                Timestamp = 1_000_000L + i,   // chronological order
                Close     = closes[i],
                Volume    = volume
            });
        }

        ctx.SaveChanges();
    }

    private static Portfolio MakePortfolio(
        string userId, string symbol,
        int totalQty, decimal avgCost) => new()
    {
        UserId            = userId,
        Symbol            = symbol,
        TotalQuantity     = totalQty,
        LockedQuantity    = 0,
        AvailableQuantity = totalQty,   // computed column — set manually
        AvgCostPrice      = avgCost,
        RowVersion        = new byte[8]
    };

    // ── GetMyPortfolioAsync — empty portfolio ─────────────────────────────────

    [Fact]
    public async Task GetMyPortfolioAsync_EmptyPortfolio_ReturnsZeroSummary()
    {
        using var ctx = BuildContext();
        var portfolioRepo = new Mock<IPortfolioRepository>();

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>());

        var svc = new PortfolioService(portfolioRepo.Object, ctx);

        var result = await svc.GetMyPortfolioAsync("user1");

        Assert.Equal(0m, result.TotalMarketValue);
        Assert.Equal(0m, result.TotalCost);
        Assert.Equal(0m, result.TotalUnrealizedPnL);
        Assert.Equal(0m, result.TotalUnrealizedPnLPercent);
        Assert.Empty(result.Holdings);
    }

    [Fact]
    public async Task GetMyPortfolioAsync_HoldingWithZeroQuantity_IsExcluded()
    {
        using var ctx = BuildContext();
        SeedSymbolAndCandles(ctx, "VIC", new[] { 80_000m });

        var portfolioRepo = new Mock<IPortfolioRepository>();

        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "VIC", totalQty: 0, avgCost: 70_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);

        var result = await svc.GetMyPortfolioAsync("user1");

        // Holdings with TotalQuantity == 0 must be filtered out
        Assert.Empty(result.Holdings);
    }

    // ── GetMyPortfolioAsync — market value calculation ────────────────────────

    [Fact]
    public async Task GetMyPortfolioAsync_MarketValue_EqualsLatestPriceTimesQuantity()
    {
        using var ctx = BuildContext();
        // Latest candle close is the last element: 100_000m
        SeedSymbolAndCandles(ctx, "FPT", new[] { 80_000m, 90_000m, 100_000m });

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "FPT", totalQty: 200, avgCost: 80_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        var holding = result.Holdings.Single();

        Assert.Equal(100_000m, holding.CurrentPrice);
        Assert.Equal(200 * 100_000m, holding.MarketValue);
    }

    // ── GetMyPortfolioAsync — unrealized PnL calculation ─────────────────────

    [Fact]
    public async Task GetMyPortfolioAsync_UnrealizedPnL_CalculatedCorrectly()
    {
        // avgCost = 80_000, latestPrice = 100_000, qty = 100
        // UnrealizedPnL = (100_000 - 80_000) * 100 = 2_000_000
        // UnrealizedPnLPercent = 2_000_000 / (80_000 * 100) * 100 = 25%
        using var ctx = BuildContext();
        SeedSymbolAndCandles(ctx, "FPT", new[] { 80_000m, 100_000m });

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "FPT", totalQty: 100, avgCost: 80_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        var holding = result.Holdings.Single();

        Assert.Equal(2_000_000m,   holding.UnrealizedPnL);
        Assert.Equal(25m,          holding.UnrealizedPnLPercent);
    }

    [Fact]
    public async Task GetMyPortfolioAsync_PriceBelowCost_NegativeUnrealizedPnL()
    {
        // avgCost = 100_000, latestPrice = 80_000, qty = 50
        // UnrealizedPnL = (80_000 - 100_000) * 50 = -1_000_000
        // UnrealizedPnLPercent = -1_000_000 / (100_000 * 50) * 100 = -20%
        using var ctx = BuildContext();
        SeedSymbolAndCandles(ctx, "VHM", new[] { 100_000m, 80_000m });

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "VHM", totalQty: 50, avgCost: 100_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        var holding = result.Holdings.Single();

        Assert.Equal(-1_000_000m, holding.UnrealizedPnL);
        Assert.Equal(-20m,        holding.UnrealizedPnLPercent);
    }

    [Fact]
    public async Task GetMyPortfolioAsync_NoCandleForSymbol_FallsBackToAvgCostPrice()
    {
        // When no candle exists, currentPrice falls back to AvgCostPrice (per service logic)
        // so UnrealizedPnL == 0
        using var ctx = BuildContext();
        // Do NOT seed any candles for TCB
        ctx.Symbols.Add(new Symbol { Symbol1 = "TCB", CompanyName = "Techcombank" });
        ctx.SaveChanges();

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "TCB", totalQty: 100, avgCost: 30_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        var holding = result.Holdings.Single();

        // currentPrice = avgCostPrice when no candle found
        Assert.Equal(30_000m, holding.CurrentPrice);
        Assert.Equal(0m,      holding.UnrealizedPnL);
    }

    // ── GetMyPortfolioAsync — portfolio totals ────────────────────────────────

    [Fact]
    public async Task GetMyPortfolioAsync_MultipleHoldings_SummarizesCorrectly()
    {
        // FPT: 100 @ cost 80_000, latest 100_000 → marketValue 10_000_000, PnL 2_000_000
        // VIC: 50  @ cost 90_000, latest 90_000  → marketValue  4_500_000, PnL        0
        using var ctx = BuildContext();
        SeedSymbolAndCandles(ctx, "FPT", new[] { 80_000m, 100_000m });
        SeedSymbolAndCandles(ctx, "VIC", new[] { 85_000m, 90_000m });

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "FPT", totalQty: 100, avgCost: 80_000m),
                         MakePortfolio("user1", "VIC", totalQty: 50,  avgCost: 90_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        Assert.Equal(2, result.Holdings.Count);
        Assert.Equal(14_500_000m, result.TotalMarketValue);   // 10M + 4.5M
        Assert.Equal(12_500_000m, result.TotalCost);          // 8M + 4.5M
        Assert.Equal(2_000_000m,  result.TotalUnrealizedPnL); // 2M + 0
    }

    [Fact]
    public async Task GetMyPortfolioAsync_UsesLatestCandleByTimestamp()
    {
        // Ensures descending order picks the correct (latest) candle
        using var ctx = BuildContext();

        // Add candles in reverse order to test that the sort is applied
        ctx.Symbols.Add(new Symbol { Symbol1 = "MSN", CompanyName = "Masan" });
        ctx.Candles.Add(new Candle { Symbol = "MSN", Timestamp = 3_000L, Close = 120_000m });
        ctx.Candles.Add(new Candle { Symbol = "MSN", Timestamp = 1_000L, Close = 80_000m });
        ctx.Candles.Add(new Candle { Symbol = "MSN", Timestamp = 2_000L, Close = 100_000m });
        ctx.SaveChanges();

        var portfolioRepo = new Mock<IPortfolioRepository>();
        portfolioRepo.Setup(r => r.GetByUserIdAsync("user1"))
                     .ReturnsAsync(new List<Portfolio>
                     {
                         MakePortfolio("user1", "MSN", totalQty: 10, avgCost: 100_000m)
                     });

        var svc = new PortfolioService(portfolioRepo.Object, ctx);
        var result = await svc.GetMyPortfolioAsync("user1");

        // Timestamp 3_000 has close 120_000 — must be picked as latest
        Assert.Equal(120_000m, result.Holdings.Single().CurrentPrice);
    }
}
