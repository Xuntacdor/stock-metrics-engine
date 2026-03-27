using backend_api.Api.Data;
using backend_api.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public interface IScreenerService
{
    Task<List<ScreenerResultDto>> FilterAsync(ScreenerFilterRequest filter);
}

public class ScreenerService : IScreenerService
{
    private readonly QuantIQContext _ctx;
    private readonly ILogger<ScreenerService> _logger;

    public ScreenerService(QuantIQContext ctx, ILogger<ScreenerService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<List<ScreenerResultDto>> FilterAsync(ScreenerFilterRequest filter)
    {
        // ── 1. Pull the two most recent closes per symbol in a single query ──
        //    We need: latest close (price), previous close (for changePct), and latest volume.
        //    Strategy: fetch last 16 candles per symbol (15 for RSI + 1 look-back for changePct).

        var symbols = await _ctx.Symbols.ToListAsync();

        var results = new List<ScreenerResultDto>(symbols.Count);

        foreach (var sym in symbols)
        {
            var candles = await _ctx.Candles
                .Where(c => c.Symbol == sym.Symbol1 && c.Close != null)
                .OrderByDescending(c => c.Timestamp)
                .Take(16)
                .Select(c => new { c.Timestamp, c.Close, c.Volume })
                .ToListAsync();

            if (candles.Count == 0) continue;

            var latestClose = candles[0].Close!.Value;
            var prevClose   = candles.Count > 1 ? candles[1].Close!.Value : latestClose;
            var latestVol   = candles[0].Volume;

            var changePct = prevClose > 0
                ? Math.Round((latestClose - prevClose) / prevClose * 100m, 2)
                : 0m;

            // ── 2. RSI-14 ────────────────────────────────────────────────────
            decimal? rsi = null;
            if (candles.Count >= 15)
            {
                var closes = candles.Select(c => c.Close!.Value).ToList();
                closes.Reverse();                         // chronological order

                var gains  = new List<decimal>();
                var losses = new List<decimal>();

                for (int i = 1; i < closes.Count; i++)
                {
                    var diff = closes[i] - closes[i - 1];
                    if (diff >= 0) { gains.Add(diff); losses.Add(0); }
                    else           { gains.Add(0);    losses.Add(-diff); }
                }

                // Use only the first 14 periods (standard Wilder RSI)
                var g14 = gains.Take(14).ToList();
                var l14 = losses.Take(14).ToList();

                var avgGain = g14.Average();
                var avgLoss = l14.Average();

                rsi = avgLoss == 0
                    ? 100m
                    : Math.Round(100m - (100m / (1m + avgGain / avgLoss)), 2);
            }

            // ── 3. Market-cap bucket ─────────────────────────────────────────
            var cap = sym.MarketCap;
            bool capMatch = filter.MarketCap switch
            {
                "large" => cap != null && cap >= 10_000m,
                "mid"   => cap != null && cap >= 1_000m && cap < 10_000m,
                "small" => cap != null && cap < 1_000m,
                _       => true   // "all"
            };

            // ── 4. Sector filter ─────────────────────────────────────────────
            var sectors = filter.Sector
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool sectorMatch = sectors.Length == 0
                || (sym.Sector != null && sectors.Contains(sym.Sector, StringComparer.OrdinalIgnoreCase));

            // ── 5. Apply numeric filters ──────────────────────────────────────
            if (latestClose < filter.PriceMin || latestClose > filter.PriceMax) continue;
            if (sym.Pe.HasValue && (sym.Pe < filter.PeMin || sym.Pe > filter.PeMax)) continue;
            if (rsi.HasValue && (rsi < filter.RsiMin || rsi > filter.RsiMax)) continue;
            if (latestVol.HasValue && latestVol < (long)filter.VolumeMin) continue;
            if (!capMatch) continue;
            if (!sectorMatch) continue;

            results.Add(new ScreenerResultDto(
                sym.Symbol1,
                sym.CompanyName,
                sym.Exchange,
                sym.Sector,
                latestClose,
                prevClose,
                changePct,
                latestVol,
                sym.Pe,
                sym.MarketCap,
                rsi
            ));
        }

        // ── 6. Sort ───────────────────────────────────────────────────────────
        results = filter.SortBy switch
        {
            "rsi"       => filter.SortDesc ? results.OrderByDescending(r => r.Rsi14).ToList()     : results.OrderBy(r => r.Rsi14).ToList(),
            "pe"        => filter.SortDesc ? results.OrderByDescending(r => r.Pe).ToList()         : results.OrderBy(r => r.Pe).ToList(),
            "marketCap" => filter.SortDesc ? results.OrderByDescending(r => r.MarketCap).ToList() : results.OrderBy(r => r.MarketCap).ToList(),
            "volume"    => filter.SortDesc ? results.OrderByDescending(r => r.Volume).ToList()    : results.OrderBy(r => r.Volume).ToList(),
            _           => filter.SortDesc ? results.OrderByDescending(r => r.ChangePct).ToList() : results.OrderBy(r => r.ChangePct).ToList(),
        };

        return results.Take(Math.Min(filter.Limit, 200)).ToList();
    }
}
