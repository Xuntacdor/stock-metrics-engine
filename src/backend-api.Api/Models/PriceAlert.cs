namespace backend_api.Api.Models;

public class PriceAlert
{
    public int AlertId { get; set; }

    public string UserId { get; set; } = null!;

    /// <summary>Stock ticker, e.g. "FPT"</summary>
    public string Symbol { get; set; } = null!;

    /// <summary>price | volume | rsi | news</summary>
    public string AlertType { get; set; } = null!;

    /// <summary>gt | gte | lt | lte</summary>
    public string Condition { get; set; } = null!;

    public decimal ThresholdValue { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsTriggered { get; set; } = false;

    /// <summary>When true the alert deactivates itself after firing once.</summary>
    public bool NotifyOnce { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? TriggeredAt { get; set; }

    public virtual User User { get; set; } = null!;
}
