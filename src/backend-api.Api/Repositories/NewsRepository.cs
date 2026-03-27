using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class NewsRepository : INewsRepository
{
    private readonly QuantIQContext _ctx;
    public NewsRepository(QuantIQContext ctx) => _ctx = ctx;

    public async Task<List<NewsArticleDto>> GetBySymbolAsync(string? symbol, int limit = 20)
    {
        var q = _ctx.NewsArticles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(symbol))
            q = q.Where(a => a.Symbol == symbol.ToUpper() || a.Symbol == null);
        return await q
            .OrderByDescending(a => a.PublishedAt)
            .Take(Math.Min(limit, 100))
            .Select(a => new NewsArticleDto(
                a.ArticleId, a.Symbol, a.Title, a.Url, a.Source,
                a.Summary, a.PublishedAt, a.Sentiment, a.SentimentScore))
            .ToListAsync();
    }

    public async Task<SentimentSummaryDto> GetSentimentSummaryAsync(string symbol, int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var articles = await _ctx.NewsArticles
            .Where(a => a.Symbol == symbol.ToUpper() && a.PublishedAt >= since && a.Sentiment != null)
            .ToListAsync();

        int total    = articles.Count;
        int bullish  = articles.Count(a => a.Sentiment == "positive");
        int bearish  = articles.Count(a => a.Sentiment == "negative");
        int neutral  = articles.Count(a => a.Sentiment == "neutral");
        double bPct  = total > 0 ? Math.Round((double)bullish / total * 100, 1) : 0;
        double rPct  = total > 0 ? Math.Round((double)bearish / total * 100, 1) : 0;
        double nPct  = total > 0 ? Math.Round((double)neutral / total * 100, 1) : 0;

        string signal = total == 0 ? "NEUTRAL"
            : bullish > bearish && bullish > neutral ? "BULLISH"
            : bearish > bullish && bearish > neutral ? "BEARISH"
            : "NEUTRAL";

        return new SentimentSummaryDto(symbol.ToUpper(), total, bullish, bearish, neutral, bPct, rPct, nPct, signal);
    }

    public async Task<List<SentimentDayDto>> GetSentimentTrendAsync(string symbol, int days = 30)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var articles = await _ctx.NewsArticles
            .Where(a => a.Symbol == symbol.ToUpper()
                     && a.PublishedAt >= since
                     && a.Sentiment != null)
            .Select(a => new { a.PublishedAt, a.Sentiment, a.SentimentScore })
            .ToListAsync();

        var grouped = articles
            .GroupBy(a => DateOnly.FromDateTime(a.PublishedAt!.Value.ToUniversalTime()))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                int total   = g.Count();
                int bullish = g.Count(a => a.Sentiment == "positive");
                int bearish = g.Count(a => a.Sentiment == "negative");
                int neutral = g.Count(a => a.Sentiment == "neutral");
                double avg  = g.Where(a => a.SentimentScore.HasValue)
                               .Select(a => a.SentimentScore!.Value)
                               .DefaultIfEmpty(0.5)
                               .Average();

                string signal = bullish > bearish && bullish > neutral ? "BULLISH"
                              : bearish > bullish && bearish > neutral ? "BEARISH"
                              : "NEUTRAL";

                return new SentimentDayDto(g.Key, total, bullish, bearish, neutral, Math.Round(avg, 4), signal);
            })
            .ToList();

        return grouped;
    }

    public async Task<bool> ExistsByUrlAsync(string url)
        => await _ctx.NewsArticles.AnyAsync(a => a.Url == url);

    public async Task UpsertAsync(NewsArticle article)
    {
        if (!await ExistsByUrlAsync(article.Url))
        {
            _ctx.NewsArticles.Add(article);
            await _ctx.SaveChangesAsync();
        }
    }
}
