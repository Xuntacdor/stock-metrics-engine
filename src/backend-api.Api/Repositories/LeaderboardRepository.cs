using backend_api.Api.Constants;
using backend_api.Api.Data;
using backend_api.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly QuantIQContext _ctx;
    public LeaderboardRepository(QuantIQContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopTradersAsync(int limit)
    {
        // Realized profit per SELL order = (AvgMatchedPrice − AvgCostPrice) × MatchedQty
        var stats = await (
            from o in _ctx.Orders
            where o.Side == OrderSide.Sell
               && (o.Status == OrderStatus.Filled || o.Status == OrderStatus.PartiallyFilled)
               && o.AvgMatchedPrice != null   // null = order was never matched
               && o.MatchedQty > 0
            join p in _ctx.Portfolios
                on new { o.UserId, o.Symbol } equals new { p.UserId, p.Symbol }
                into ps
            from p in ps.DefaultIfEmpty()
            select new
            {
                o.UserId,
                Profit = ((o.AvgMatchedPrice ?? 0m) - (p != null ? p.AvgCostPrice : 0m)) * (decimal)o.MatchedQty,
            })
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId      = g.Key,
                RealizedPnL = g.Sum(x => x.Profit),
                TradeCount  = g.Count(),
            })
            .OrderByDescending(x => x.RealizedPnL)
            .Take(limit)
            .ToListAsync();

        var userIds = stats.Select(s => s.UserId).ToList();

        var users = await _ctx.Users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Username })
            .ToDictionaryAsync(u => u.UserId);

        // Total BUY cost per user as the ROI denominator
        var buySums = await _ctx.Transactions
            .Where(t => t.TransType == TransactionType.Buy && userIds.Contains(t.UserId))
            .GroupBy(t => t.UserId)
            .Select(g => new { UserId = g.Key, TotalBuy = g.Sum(t => -t.Amount) })
            .ToDictionaryAsync(x => x.UserId);

        return stats.Select((s, i) =>
        {
            users.TryGetValue(s.UserId, out var user);
            buySums.TryGetValue(s.UserId, out var buy);
            var totalBuy = buy?.TotalBuy ?? 0;
            var pct = totalBuy > 0
                ? Math.Round((double)s.RealizedPnL / (double)totalBuy * 100, 2)
                : 0;
            return new LeaderboardEntryDto(
                Rank:           i + 1,
                UserId:         s.UserId,
                Username:       user?.Username ?? "Trader",
                RealizedPnL:    s.RealizedPnL,
                RealizedPnLPct: (decimal)pct,
                TradeCount:     s.TradeCount
            );
        }).ToList();
    }
}
