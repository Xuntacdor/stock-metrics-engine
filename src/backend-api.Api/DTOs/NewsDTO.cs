namespace backend_api.Api.DTOs;

public record NewsArticleDto(
    int ArticleId,
    string? Symbol,
    string Title,
    string Url,
    string? Source,
    string? Summary,
    DateTime? PublishedAt,
    string? Sentiment,
    double? SentimentScore
);

public record SentimentSummaryDto(
    string Symbol,
    int Total,
    int Bullish,
    int Bearish,
    int Neutral,
    double BullishPct,
    double BearishPct,
    double NeutralPct,
    string OverallSignal   // "BULLISH" | "BEARISH" | "NEUTRAL"
);

public record CommentDto(
    int CommentId,
    string Symbol,
    string UserId,
    string Username,
    string Content,
    DateTime CreatedAt
);

public record CreateCommentRequest(string Content);

public record LeaderboardEntryDto(
    int Rank,
    string UserId,
    string Username,
    decimal RealizedPnL,
    decimal RealizedPnLPct,
    int TradeCount
);
