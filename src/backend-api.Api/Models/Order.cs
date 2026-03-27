using backend_api.Api.Constants;

namespace backend_api.Api.Models;

public partial class Order
{
    public string OrderId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Symbol { get; set; } = null!;

    public string Side { get; set; } = null!;

    public string OrderType { get; set; } = null!;

    public string Status { get; set; } = OrderStatus.Pending;

    public int RequestQty { get; set; }

    public decimal Price { get; set; }

    public int MatchedQty { get; set; }

    /// <summary>Null while the order is PENDING (not yet matched).</summary>
    public decimal? AvgMatchedPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Symbol SymbolNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
