using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly QuantIQContext _context;

    public PortfolioService(IPortfolioRepository portfolioRepo, QuantIQContext context)
    {
        _portfolioRepo = portfolioRepo;
        _context = context;
    }

    public async Task<PortfolioSummaryResponse> GetMyPortfolioAsync(string userId)
    {
        var holdings = (await _portfolioRepo.GetByUserIdAsync(userId))
            .Where(p => p.TotalQuantity > 0)
            .ToList();

        var items = new List<PortfolioItemResponse>();

        foreach (var h in holdings)
        {
            // Get latest close price from Candles table
            var latestCandle = await _context.Candles
                .Where(c => c.Symbol == h.Symbol)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();

            var currentPrice = latestCandle?.Close ?? h.AvgCostPrice ?? 0;
            var avgCost = h.AvgCostPrice ?? 0;
            var qty = h.TotalQuantity ?? 0;
            var marketValue = currentPrice * qty;
            var unrealizedPnL = (currentPrice - avgCost) * qty;
            var costBasis = avgCost * qty;
            var unrealizedPct = costBasis != 0 ? (unrealizedPnL / costBasis) * 100 : 0;

            items.Add(new PortfolioItemResponse
            {
                Symbol = h.Symbol,
                TotalQuantity = qty,
                AvailableQuantity = h.AvailableQuantity ?? 0,
                LockedQuantity = h.LockedQuantity ?? 0,
                AvgCostPrice = avgCost,
                CurrentPrice = currentPrice,
                MarketValue = marketValue,
                UnrealizedPnL = unrealizedPnL,
                UnrealizedPnLPercent = Math.Round(unrealizedPct, 2)
            });
        }

        var totalMarketValue = items.Sum(i => i.MarketValue);
        var totalCost = items.Sum(i => i.AvgCostPrice * i.TotalQuantity);
        var totalPnL = items.Sum(i => i.UnrealizedPnL);
        var totalPnLPct = totalCost != 0 ? (totalPnL / totalCost) * 100 : 0;

        return new PortfolioSummaryResponse
        {
            TotalMarketValue = totalMarketValue,
            TotalCost = totalCost,
            TotalUnrealizedPnL = totalPnL,
            TotalUnrealizedPnLPercent = Math.Round(totalPnLPct, 2),
            Holdings = items
        };
    }
}
