using backend_api.Api.Data;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="LeaderboardRepository"/>.
///
/// The repository executes a LINQ query that:
///   1. Joins SELL/FILLED Orders with Portfolios (DefaultIfEmpty).
///   2. Groups by UserId and sums realized PnL = (AvgMatchedPrice - AvgCostPrice) * MatchedQty.
///   3. Looks up usernames from Users.
///   4. Looks up total BUY cost from Transactions (BUY type, Amount is negative).
///   5. Computes ROI% = RealizedPnL / TotalBuyCost * 100.
///
/// Notes on InMemory EF:
///   - AvailableBalance on CashWallet and AvailableQuantity on Portfolio are
///     computed columns in SQL Server — they must be set manually in seed data.
///   - Portfolio.RowVersion must be set to a non-null byte array.
/// </summary>
public class LeaderboardRepositoryTests
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

    private static User MakeUser(string userId, string username) => new()
    {
        UserId       = userId,
        Username     = username,
        PasswordHash = "hash"
    };

    private static Symbol MakeSymbol(string sym) => new()
    {
        Symbol1     = sym,
        CompanyName = sym + " Corp"
    };

    private static Portfolio MakePortfolio(
        string userId, string symbol, decimal avgCost, int totalQty = 100) => new()
    {
        UserId            = userId,
        Symbol            = symbol,
        TotalQuantity     = totalQty,
        LockedQuantity    = 0,
        AvailableQuantity = totalQty,   // computed column — set manually
        AvgCostPrice      = avgCost,
        RowVersion        = new byte[8]
    };

    private static Order MakeSellOrder(
        string orderId,
        string userId,
        string symbol,
        int matchedQty,
        decimal avgMatchedPrice,
        string status = "FILLED") => new()
    {
        OrderId          = orderId,
        UserId           = userId,
        Symbol           = symbol,
        Side             = "SELL",
        OrderType        = "LO",
        Status           = status,
        RequestQty       = matchedQty,
        Price            = avgMatchedPrice,
        MatchedQty       = matchedQty,
        AvgMatchedPrice  = avgMatchedPrice,
        CreatedAt        = DateTime.UtcNow
    };

    /// <summary>
    /// Creates a BUY transaction record. The service uses -t.Amount to sum buy cost,
    /// so Amount should be negative (cash outflow).
    /// </summary>
    private static Transaction MakeBuyTransaction(
        long transId,
        string userId,
        decimal amount) => new()
    {
        TransId       = transId,
        RefId         = $"BUY-{transId}",
        UserId        = userId,
        TransType     = "BUY",
        Amount        = amount,   // negative = cash spent
        BalanceBefore = 1_000_000m,
        BalanceAfter  = 1_000_000m + amount,
        TransTime     = DateTime.UtcNow
    };

    // ── GetTopTradersAsync — basic ranking ────────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_SingleTrader_ReturnsOneEntry()
    {
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m, totalQty: 100));
        ctx.Orders.Add(MakeSellOrder("O1", "u1", "FPT",
            matchedQty: 100, avgMatchedPrice: 100_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        Assert.Equal("u1",    result[0].UserId);
        Assert.Equal("Alice", result[0].Username);
        Assert.Equal(1,       result[0].Rank);
    }

    [Fact]
    public async Task GetTopTradersAsync_RankedByRealizedPnLDescending()
    {
        // Alice: (100_000 - 80_000) * 100 = 2_000_000
        // Bob:   (100_000 - 90_000) * 100 =   500_000
        using var ctx = BuildContext();

        ctx.Users.AddRange(
            MakeUser("alice", "Alice"),
            MakeUser("bob",   "Bob"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.AddRange(
            MakePortfolio("alice", "FPT", avgCost: 80_000m),
            MakePortfolio("bob",   "FPT", avgCost: 90_000m));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "alice", "FPT", 100, 100_000m),
            MakeSellOrder("O2", "bob",   "FPT", 100, 100_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Equal(2,        result.Count);
        Assert.Equal("alice",  result[0].UserId);   // highest PnL
        Assert.Equal("bob",    result[1].UserId);
        Assert.True(result[0].RealizedPnL > result[1].RealizedPnL);
    }

    // ── GetTopTradersAsync — rank values ──────────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_RankStartsAtOne_IsSequential()
    {
        using var ctx = BuildContext();

        ctx.Users.AddRange(
            MakeUser("u1", "Alice"),
            MakeUser("u2", "Bob"),
            MakeUser("u3", "Charlie"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.AddRange(
            MakePortfolio("u1", "FPT", avgCost: 70_000m),
            MakePortfolio("u2", "FPT", avgCost: 80_000m),
            MakePortfolio("u3", "FPT", avgCost: 90_000m));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "u1", "FPT", 50, 100_000m),
            MakeSellOrder("O2", "u2", "FPT", 50, 100_000m),
            MakeSellOrder("O3", "u3", "FPT", 50, 100_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(2, result[1].Rank);
        Assert.Equal(3, result[2].Rank);
    }

    // ── GetTopTradersAsync — limit ────────────────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_LimitOf1_ReturnsOnlyTopTrader()
    {
        using var ctx = BuildContext();

        ctx.Users.AddRange(
            MakeUser("u1", "Alice"),
            MakeUser("u2", "Bob"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.AddRange(
            MakePortfolio("u1", "FPT", avgCost: 80_000m),
            MakePortfolio("u2", "FPT", avgCost: 90_000m));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "u1", "FPT", 100, 100_000m),   // PnL = 2_000_000
            MakeSellOrder("O2", "u2", "FPT", 100, 100_000m));  // PnL =   500_000 (lower)
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(1);

        Assert.Single(result);
        Assert.Equal("u1", result[0].UserId);
    }

    [Fact]
    public async Task GetTopTradersAsync_LimitOf2_Returns2WhenThreeExist()
    {
        using var ctx = BuildContext();

        ctx.Users.AddRange(
            MakeUser("u1", "Alice"),
            MakeUser("u2", "Bob"),
            MakeUser("u3", "Charlie"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.AddRange(
            MakePortfolio("u1", "FPT", avgCost: 60_000m),
            MakePortfolio("u2", "FPT", avgCost: 70_000m),
            MakePortfolio("u3", "FPT", avgCost: 80_000m));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "u1", "FPT", 100, 100_000m),
            MakeSellOrder("O2", "u2", "FPT", 100, 100_000m),
            MakeSellOrder("O3", "u3", "FPT", 100, 100_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(2);

        Assert.Equal(2, result.Count);
    }

    // ── GetTopTradersAsync — DefaultIfEmpty (no portfolio entry) ─────────────

    [Fact]
    public async Task GetTopTradersAsync_UserWithNoPortfolio_DefaultIfEmptyMakesAvgCostZero()
    {
        // The JOIN to Portfolios uses DefaultIfEmpty, so p.AvgCostPrice defaults to 0.
        // Realized PnL = (AvgMatchedPrice - 0) * MatchedQty = full sale price as profit.
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "NoPortfolio"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        // Deliberately do NOT add a Portfolio row for u1
        ctx.Orders.Add(MakeSellOrder("O1", "u1", "FPT",
            matchedQty: 10, avgMatchedPrice: 50_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        // With no portfolio, avgCost = 0, so realizedPnL = 50_000 * 10 = 500_000
        Assert.Equal(500_000m, result[0].RealizedPnL);
    }

    // ── GetTopTradersAsync — only SELL FILLED orders are counted ─────────────

    [Fact]
    public async Task GetTopTradersAsync_BuyOrdersAreIgnored_NotCountedInPnL()
    {
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m));

        // BUY order — must be excluded
        ctx.Orders.Add(new Order
        {
            OrderId         = "BUY1",
            UserId          = "u1",
            Symbol          = "FPT",
            Side            = "BUY",
            OrderType       = "LO",
            Status          = "FILLED",
            RequestQty      = 100,
            Price           = 100_000m,
            MatchedQty      = 100,
            AvgMatchedPrice = 100_000m,
            CreatedAt       = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        // No SELL orders → leaderboard is empty
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTopTradersAsync_PendingOrders_AreIgnored()
    {
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m));
        ctx.Orders.Add(MakeSellOrder("O1", "u1", "FPT",
            matchedQty: 0, avgMatchedPrice: 100_000m, status: "PENDING"));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        // PENDING orders are excluded by the WHERE clause
        Assert.Empty(result);
    }

    // ── GetTopTradersAsync — ROI calculation ──────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_RoiPercent_CalculatedCorrectly()
    {
        // RealizedPnL = (100_000 - 80_000) * 100 = 2_000_000
        // TotalBuyCost = -(-8_000_000) = 8_000_000  (Amount = -8_000_000)
        // ROI% = 2_000_000 / 8_000_000 * 100 = 25%
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m, totalQty: 100));
        ctx.Orders.Add(MakeSellOrder("O1", "u1", "FPT", 100, 100_000m));
        ctx.Transactions.Add(MakeBuyTransaction(1, "u1", -8_000_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        Assert.Equal(25m, result[0].RealizedPnLPct);
    }

    [Fact]
    public async Task GetTopTradersAsync_NoBuyTransactions_RoiIsZero()
    {
        // When there are no BUY transactions, totalBuy = 0 and ROI% defaults to 0
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m));
        ctx.Orders.Add(MakeSellOrder("O1", "u1", "FPT", 100, 100_000m));
        // Deliberately no Transaction rows
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        Assert.Equal(0m, result[0].RealizedPnLPct);
    }

    // ── GetTopTradersAsync — empty database ───────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_EmptyDatabase_ReturnsEmptyList()
    {
        using var ctx = BuildContext();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Empty(result);
    }

    // ── GetTopTradersAsync — username fallback ────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_UnknownUserId_FallsBackToTraderUsername()
    {
        // The user row exists in Orders but NOT in Users table (orphan data scenario).
        // The service uses user?.Username ?? "Trader" as the fallback.
        using var ctx = BuildContext();

        // Do NOT add the user to ctx.Users
        ctx.Symbols.Add(MakeSymbol("FPT"));
        // We cannot reference a non-existent User FK due to EF navigation tracking,
        // so we manually manipulate via context with FK bypassed (InMemory has no FK enforcement).
        ctx.Orders.Add(new Order
        {
            OrderId         = "O1",
            UserId          = "ghost-user",
            Symbol          = "FPT",
            Side            = "SELL",
            OrderType       = "LO",
            Status          = "FILLED",
            RequestQty      = 50,
            Price           = 100_000m,
            MatchedQty      = 50,
            AvgMatchedPrice = 100_000m
        });
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        Assert.Equal("Trader", result[0].Username);
    }

    // ── GetTopTradersAsync — trade count ──────────────────────────────────────

    [Fact]
    public async Task GetTopTradersAsync_MultipleOrdersSameUser_SumsTradeCount()
    {
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m, totalQty: 300));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "u1", "FPT", 100, 100_000m),
            MakeSellOrder("O2", "u1", "FPT", 100, 110_000m),
            MakeSellOrder("O3", "u1", "FPT", 100, 120_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Single(result);
        Assert.Equal(3, result[0].TradeCount);
    }

    [Fact]
    public async Task GetTopTradersAsync_MultipleOrdersSameUser_SumsRealizedPnLAcrossOrders()
    {
        // Three sell orders at different prices:
        // O1: (100_000 - 80_000) * 100 = 2_000_000
        // O2: (110_000 - 80_000) * 100 = 3_000_000
        // O3: (120_000 - 80_000) * 100 = 4_000_000
        // Total = 9_000_000
        using var ctx = BuildContext();

        ctx.Users.Add(MakeUser("u1", "Alice"));
        ctx.Symbols.Add(MakeSymbol("FPT"));
        ctx.Portfolios.Add(MakePortfolio("u1", "FPT", avgCost: 80_000m, totalQty: 300));
        ctx.Orders.AddRange(
            MakeSellOrder("O1", "u1", "FPT", 100, 100_000m),
            MakeSellOrder("O2", "u1", "FPT", 100, 110_000m),
            MakeSellOrder("O3", "u1", "FPT", 100, 120_000m));
        await ctx.SaveChangesAsync();

        var repo   = new LeaderboardRepository(ctx);
        var result = await repo.GetTopTradersAsync(10);

        Assert.Equal(9_000_000m, result[0].RealizedPnL);
    }
}
