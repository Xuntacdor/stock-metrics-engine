namespace backend_api.Api.Models;

public class NewsArticle
{
    public int ArticleId { get; set; }
    public string? Symbol { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Summary { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Sentiment { get; set; }     // positive / negative / neutral
    public double? SentimentScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
