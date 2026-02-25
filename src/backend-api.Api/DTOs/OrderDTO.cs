namespace backend_api.Api.DTOs;

public class PlaceOrderRequest
{
    public string Symbol { get; set; } = null!;
    public string Side { get; set; } = null!;
    public string OrderType { get; set; } = "MARKET"; 
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderResponse
{
    public string OrderId { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public string Side { get; set; } = null!;
    public string OrderType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int RequestQty { get; set; }
    public int? MatchedQty { get; set; }
    public decimal Price { get; set; }
    public decimal? AvgMatchedPrice { get; set; }
    public DateTime? CreatedAt { get; set; }
}
