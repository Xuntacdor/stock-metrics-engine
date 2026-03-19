namespace backend_api.Api.Models;

public class StockComment
{
    public int CommentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
