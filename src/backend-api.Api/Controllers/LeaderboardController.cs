using backend_api.Api.Data;
using backend_api.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly QuantIQContext _ctx;
    public LeaderboardController(QuantIQContext ctx) => _ctx = ctx;

    /// <summary>
    /// GET /api/leaderboard?limit=20
    /// Top traders ranked by Realized P&amp;L from SELL transactions.
    /// Realized PnL per trade = (SellPrice - AvgCost) × Qty − fees
    /// For simplicity we compute it from Transaction amounts where TransType=SELL (positive tx amount = realized gain).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 20)
    {
        limit = Math.Min(limit, 100);

        // Aggregate SELL transactions per user
        var stats = await _ctx.Transactions
            .Where(t => t.TransType == "SELL")
            .GroupBy(t => t.UserId)
            .Select(g => new
            {
                UserId     = g.Key,
                RealizedPnL = g.Sum(t => t.Amount),
                TradeCount  = g.Count(),
            })
            .OrderByDescending(x => x.RealizedPnL)
            .Take(limit)
            .ToListAsync();

        // Join with users for username
        var userIds = stats.Select(s => s.UserId).ToList();
        var users = await _ctx.Users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.Username })
            .ToDictionaryAsync(u => u.UserId);

        // Calculate total invest base from BUY transactions for each user to compute %
        var buySums = await _ctx.Transactions
            .Where(t => t.TransType == "BUY" && userIds.Contains(t.UserId))
            .GroupBy(t => t.UserId)
            .Select(g => new { UserId = g.Key, TotalBuy = g.Sum(t => -t.Amount) }) // BUY amounts are negative
            .ToDictionaryAsync(x => x.UserId);

        var result = stats.Select((s, i) =>
        {
            users.TryGetValue(s.UserId, out var user);
            buySums.TryGetValue(s.UserId, out var buy);
            var totalBuy = buy?.TotalBuy ?? 0;
            var pct = totalBuy > 0 ? Math.Round((double)s.RealizedPnL / (double)totalBuy * 100, 2) : 0;
            return new LeaderboardEntryDto(
                Rank:           i + 1,
                UserId:         s.UserId,
                Username:       user?.Username ?? "Trader",
                RealizedPnL:    s.RealizedPnL,
                RealizedPnLPct: (decimal)pct,
                TradeCount:     s.TradeCount
            );
        }).ToList();

        return Ok(result);
    }
}
