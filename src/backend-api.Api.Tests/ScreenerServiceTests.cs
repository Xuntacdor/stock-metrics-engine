using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="ScreenerService"/>.
/// Uses an InMemory <see cref="QuantIQContext"/> seeded with Symbols and Candles.
/// Each test gets an isolated database name (Guid) to prevent cross-test interference.
///
/// RSI calculation notes (from the service):
///   - Requires >= 15 candles per symbol.
///   - Candles are fetched ORDER BY Timestamp DESC, so the last item added
///     with the highest Timestamp is the "latest" candle.
///   - The service reverses them to chronological order before computing diffs.
/// </summary>
public class ScreenerServiceTests
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

    private static ScreenerService BuildService(QuantIQContext ctx)
        => new ScreenerService(ctx, NullLogger<ScreenerService>.Instance);

    /// <summary>
    /// Seeds one Symbol and N candles with the given close prices (chronological order).
    /// The last entry in the array is the "latest" price because it gets the highest Timestamp.
    /// </summary>
    private static void SeedSymbol(
        QuantIQContext ctx,
        string symbol,
        decimal[] closes,
        long? volume    = 100_000L,
        string? sector  = null,
        decimal? pe     = null,
        decimal? marketCap = null)
    {
        ctx.Symbols.Add(new Symbol
        {
            Symbol1     = symbol,
            CompanyName = symbol + " Corp",
            Exchange    = "HOSE",
            Sector      = sector,
            Pe          = pe,
            MarketCap   = marketCap
        });

        for (int i = 0; i < closes.Length; i++)
        {
            ctx.Candles.Add(new Candle
            {
                Symbol    = symbol,
                Timestamp = 1_000L + i,   // ascending = chronological
                Close     = closes[i],
                Volume    = volume
            });
        }

        ctx.SaveChanges();
    }

    /// <summary>
    /// Generates 16 candles for a symbol in a pattern that produces a known RSI.
    /// All gains: closing prices strictly increasing → RSI will be near 100.
    /// </summary>
    private static decimal[] AllGainsCloses(int count = 16, decimal start = 10m, decimal step = 1m)
    {
        var closes = new decimal[count];
        for (int i = 0; i < count; i++)
            closes[i] = start + i * step;
        return closes;
    }

    /// <summary>
    /// Generates 16 candles with strictly declining closes → RSI near 0.
    /// </summary>
    private static decimal[] AllLossesCloses(int count = 16, decimal start = 100m, decimal step = 1m)
    {
        var closes = new decimal[count];
        for (int i = 0; i < count; i++)
            closes[i] = start - i * step;
        return closes;
    }

    private static ScreenerFilterRequest DefaultFilter() => new()
    {
        PriceMin = 0,
        PriceMax = decimal.MaxValue,
        PeMin    = 0,
        PeMax    = decimal.MaxValue,
        RsiMin   = 0,
        RsiMax   = 100,
        VolumeMin = 0,
        MarketCap = "all",
        Sector    = string.Empty,
        SortBy    = "changePct",
        SortDesc  = true,
        Limit     = 200
    };

    // ── Basic pass-through ────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_NoFilters_ReturnsAllSymbolsWithCandles()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 80m, 100m });
        SeedSymbol(ctx, "VIC", new[] { 50m, 60m });

        var svc = BuildService(ctx);
        var result = await svc.FilterAsync(DefaultFilter());

        Assert.Equal(2, result.Count);
        var symbols = result.Select(r => r.Symbol).ToHashSet();
        Assert.Contains("FPT", symbols);
        Assert.Contains("VIC", symbols);
    }

    [Fact]
    public async Task FilterAsync_SymbolWithNoCandles_IsExcluded()
    {
        using var ctx = BuildContext();
        // VNM has no candles
        ctx.Symbols.Add(new Symbol { Symbol1 = "VNM", CompanyName = "Vinamilk" });
        ctx.SaveChanges();

        SeedSymbol(ctx, "FPT", new[] { 100m });

        var svc = BuildService(ctx);
        var result = await svc.FilterAsync(DefaultFilter());

        Assert.Single(result);
        Assert.Equal("FPT", result[0].Symbol);
    }

    // ── Price filter ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_PriceMinFilter_ExcludesSymbolsBelowMin()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 90m, 100m });    // latest = 100 → passes
        SeedSymbol(ctx, "VIC", new[] { 30m, 40m });     // latest = 40  → excluded

        var filter = DefaultFilter();
        filter.PriceMin = 50m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("FPT", result[0].Symbol);
    }

    [Fact]
    public async Task FilterAsync_PriceMaxFilter_ExcludesSymbolsAboveMax()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 90m, 100m });   // latest = 100 → excluded
        SeedSymbol(ctx, "VIC", new[] { 30m, 40m });    // latest = 40  → passes

        var filter = DefaultFilter();
        filter.PriceMax = 50m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("VIC", result[0].Symbol);
    }

    [Fact]
    public async Task FilterAsync_PriceRangeFilter_OnlyIncludesSymbolsInRange()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m });   // 100 — passes
        SeedSymbol(ctx, "VIC", new[] { 200m });   // 200 — excluded
        SeedSymbol(ctx, "MSN", new[] { 50m });    //  50 — excluded

        var filter = DefaultFilter();
        filter.PriceMin = 80m;
        filter.PriceMax = 150m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("FPT", result[0].Symbol);
    }

    // ── Sector filter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_SectorFilter_OnlyIncludesMatchingSector()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m }, sector: "Technology");
        SeedSymbol(ctx, "VIC", new[] { 60m },  sector: "Real Estate");

        var filter = DefaultFilter();
        filter.Sector = "Technology";

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("FPT", result[0].Symbol);
    }

    [Fact]
    public async Task FilterAsync_MultipleSectorFilter_CommaSeparated_IncludesBoth()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m }, sector: "Technology");
        SeedSymbol(ctx, "VIC", new[] { 60m },  sector: "Real Estate");
        SeedSymbol(ctx, "MSN", new[] { 80m },  sector: "Consumer");

        var filter = DefaultFilter();
        filter.Sector = "Technology,Real Estate";

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Equal(2, result.Count);
        var symbols = result.Select(r => r.Symbol).ToHashSet();
        Assert.Contains("FPT", symbols);
        Assert.Contains("VIC", symbols);
        Assert.DoesNotContain("MSN", symbols);
    }

    [Fact]
    public async Task FilterAsync_EmptySectorFilter_ReturnsAllSymbols()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m }, sector: "Technology");
        SeedSymbol(ctx, "VIC", new[] { 60m },  sector: "Real Estate");

        var filter = DefaultFilter();
        filter.Sector = string.Empty;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Equal(2, result.Count);
    }

    // ── RSI filter ────────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_RsiFilter_SymbolWith15Candles_HasRsiCalculated()
    {
        using var ctx = BuildContext();
        // 16 strictly rising candles → RSI ≈ 100
        SeedSymbol(ctx, "FPT", AllGainsCloses(16));

        var filter = DefaultFilter();
        // Accept only RSI >= 90 to confirm the RSI is computed and near 100
        filter.RsiMin = 90m;
        filter.RsiMax = 100m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("FPT", result[0].Symbol);
        Assert.NotNull(result[0].Rsi14);
        Assert.True(result[0].Rsi14 >= 90m);
    }

    [Fact]
    public async Task FilterAsync_RsiFilter_SymbolWithFewerThan15Candles_HasNullRsi_PassesFilter()
    {
        // With < 15 candles, RSI is null. The service only filters on RSI when
        // rsi.HasValue, so a null RSI should pass the filter regardless of min/max.
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 80m, 100m });   // only 2 candles → null RSI

        var filter = DefaultFilter();
        filter.RsiMin = 30m;
        filter.RsiMax = 70m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        // RSI is null so the filter does not exclude FPT
        Assert.Single(result);
        Assert.Null(result[0].Rsi14);
    }

    [Fact]
    public async Task FilterAsync_RsiMaxFilter_ExcludesHighRsiSymbols()
    {
        using var ctx = BuildContext();
        // 16 strictly rising candles → RSI ≈ 100 → should be excluded by RsiMax = 70
        SeedSymbol(ctx, "OVERBOUGHT", AllGainsCloses(16));
        // 16 declining candles → RSI ≈ 0 → should pass RsiMax = 70
        SeedSymbol(ctx, "OVERSOLD", AllLossesCloses(16));

        var filter = DefaultFilter();
        filter.RsiMin = 0m;
        filter.RsiMax = 70m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        // Only OVERSOLD should pass (very low RSI)
        var symbols = result.Select(r => r.Symbol).ToHashSet();
        Assert.DoesNotContain("OVERBOUGHT", symbols);
        Assert.Contains("OVERSOLD", symbols);
    }

    // ── Sort ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_SortByVolumeDescending_ReturnsHighestVolumeFirst()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "LOW",  new[] { 50m }, volume: 1_000L);
        SeedSymbol(ctx, "HIGH", new[] { 50m }, volume: 9_000L);
        SeedSymbol(ctx, "MID",  new[] { 50m }, volume: 5_000L);

        var filter = DefaultFilter();
        filter.SortBy   = "volume";
        filter.SortDesc = true;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Equal(3, result.Count);
        Assert.Equal("HIGH", result[0].Symbol);
        Assert.Equal("MID",  result[1].Symbol);
        Assert.Equal("LOW",  result[2].Symbol);
    }

    [Fact]
    public async Task FilterAsync_SortByVolumeAscending_ReturnsLowestVolumeFirst()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "LOW",  new[] { 50m }, volume: 1_000L);
        SeedSymbol(ctx, "HIGH", new[] { 50m }, volume: 9_000L);

        var filter = DefaultFilter();
        filter.SortBy   = "volume";
        filter.SortDesc = false;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Equal("LOW",  result[0].Symbol);
        Assert.Equal("HIGH", result[1].Symbol);
    }

    [Fact]
    public async Task FilterAsync_SortByChangePctDescending_ReturnsHighestGainerFirst()
    {
        using var ctx = BuildContext();
        // FPT: prev=80, latest=100 → changePct = +25%
        SeedSymbol(ctx, "FPT", new[] { 80m, 100m });
        // VIC: prev=100, latest=90 → changePct = -10%
        SeedSymbol(ctx, "VIC", new[] { 100m, 90m });

        var filter = DefaultFilter();
        filter.SortBy   = "changePct";
        filter.SortDesc = true;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Equal("FPT", result[0].Symbol);
        Assert.Equal("VIC", result[1].Symbol);
    }

    // ── Limit ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_LimitOf1_ReturnsOnlyOneResult()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m });
        SeedSymbol(ctx, "VIC", new[] { 60m });
        SeedSymbol(ctx, "MSN", new[] { 80m });

        var filter = DefaultFilter();
        filter.Limit = 1;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
    }

    // ── Empty results ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_NoSymbolsMatchFilter_ReturnsEmptyList()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "FPT", new[] { 100m });

        var filter = DefaultFilter();
        filter.PriceMin = 200m;   // all symbols below this — nothing passes

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterAsync_NoSymbolsInDatabase_ReturnsEmptyList()
    {
        using var ctx = BuildContext();   // empty database

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(DefaultFilter());

        Assert.Empty(result);
    }

    // ── Volume filter ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_VolumeMinFilter_ExcludesLowVolumeSymbols()
    {
        using var ctx = BuildContext();
        SeedSymbol(ctx, "HIGH", new[] { 100m }, volume: 100_000L);
        SeedSymbol(ctx, "LOW",  new[] { 100m }, volume: 5_000L);

        var filter = DefaultFilter();
        filter.VolumeMin = 50_000m;

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(filter);

        Assert.Single(result);
        Assert.Equal("HIGH", result[0].Symbol);
    }

    // ── Candle data mapped correctly ──────────────────────────────────────────

    [Fact]
    public async Task FilterAsync_LatestAndPrevCloseCorrect_ChangePctCalculated()
    {
        using var ctx = BuildContext();
        // Candles: prev=80, latest=100 → changePct = (100-80)/80*100 = 25%
        SeedSymbol(ctx, "FPT", new[] { 80m, 100m });

        var svc    = BuildService(ctx);
        var result = await svc.FilterAsync(DefaultFilter());

        var fpt = result.Single();
        Assert.Equal(100m,  fpt.LastClose);
        Assert.Equal(80m,   fpt.PrevClose);
        Assert.Equal(25m,   fpt.ChangePct);
    }
}
