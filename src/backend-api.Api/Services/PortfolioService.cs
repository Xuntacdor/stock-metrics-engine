using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly QuantIQContext _context;
    private readonly ICacheService _cache;

    public PortfolioService(IPortfolioRepository portfolioRepo, QuantIQContext context, ICacheService cache)
    {
        _portfolioRepo = portfolioRepo;
        _context = context;
        _cache = cache;
    }

    public async Task<PortfolioSummaryResponse> GetMyPortfolioAsync(string userId)
    {
        var holdings = (await _portfolioRepo.GetByUserIdAsync(userId))
            .Where(p => p.TotalQuantity > 0)
            .ToList();

        var items = new List<PortfolioItemResponse>();

        foreach (var h in holdings)
        {
            var cacheKey = $"price:latest:{h.Symbol}";
            decimal currentPrice;
            var cachedStr = await _cache.GetAsync<string>(cacheKey);
            if (cachedStr != null && decimal.TryParse(cachedStr, out var parsedPrice))
            {
                currentPrice = parsedPrice;
            }
            else
            {
                var latestCandle = await _context.Candles
                    .Where(c => c.Symbol == h.Symbol)
                    .OrderByDescending(c => c.Timestamp)
                    .FirstOrDefaultAsync();
                currentPrice = latestCandle?.Close ?? h.AvgCostPrice;
                await _cache.SetAsync(cacheKey, currentPrice.ToString(), TimeSpan.FromSeconds(30));
            }
            var avgCost = h.AvgCostPrice;
            var qty = h.TotalQuantity;
            var marketValue = currentPrice * qty;
            var unrealizedPnL = (currentPrice - avgCost) * qty;
            var costBasis = avgCost * qty;
            var unrealizedPct = costBasis != 0 ? (unrealizedPnL / costBasis) * 100 : 0;

            items.Add(new PortfolioItemResponse
            {
                Symbol = h.Symbol,
                TotalQuantity = qty,
                AvailableQuantity = h.AvailableQuantity,
                LockedQuantity = h.LockedQuantity,
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

    public async Task<RealizedPnLResponse> GetRealizedPnLAsync(string userId)
    {
        var sellTransactions = await _context.Transactions
            .Where(t => t.UserId == userId && t.TransType == "SELL" && t.Symbol != null)
            .OrderByDescending(t => t.TransTime)
            .ToListAsync();

        var trades = sellTransactions.Select(t =>
        {
            var fee = t.Fee ?? 0m;
            var tax = t.Tax ?? 0m;
            var grossPnL = t.Amount;
            var netPnL = grossPnL - fee - tax;
            return new RealizedPnLItemResponse
            {
                Symbol = t.Symbol,
                Quantity = t.Quantity,
                Price = t.Price,
                Amount = t.Amount,
                Fee = fee,
                Tax = tax,
                RealizedPnL = netPnL,
                TransTime = t.TransTime
            };
        }).ToList();

        return new RealizedPnLResponse
        {
            TotalRealizedPnL = trades.Sum(t => t.Amount),
            TotalFees = trades.Sum(t => t.Fee),
            TotalTaxes = trades.Sum(t => t.Tax),
            NetRealizedPnL = trades.Sum(t => t.RealizedPnL),
            Trades = trades
        };
    }
}
