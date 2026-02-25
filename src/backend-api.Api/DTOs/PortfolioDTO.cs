namespace backend_api.Api.DTOs;

public class PortfolioItemResponse
{
    public string Symbol { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int LockedQuantity { get; set; }
    public decimal AvgCostPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal UnrealizedPnLPercent { get; set; }
}

public class PortfolioSummaryResponse
{
    public decimal TotalMarketValue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public decimal TotalUnrealizedPnLPercent { get; set; }
    public List<PortfolioItemResponse> Holdings { get; set; } = new();
}
