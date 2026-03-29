using backend_api.Api.DTOs;
using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface INewsRepository
{
    Task<List<NewsArticleDto>> GetBySymbolAsync(string? symbol, int limit = 20);
    Task<SentimentSummaryDto> GetSentimentSummaryAsync(string symbol, int days = 7);
    Task<List<SentimentDayDto>> GetSentimentTrendAsync(string symbol, int days = 30);
    Task<bool> ExistsByUrlAsync(string url);
    Task UpsertAsync(NewsArticle article);
    Task<List<NewsArticleDto>> SearchAsync(string query, int limit = 20);
}
