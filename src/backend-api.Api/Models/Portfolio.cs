namespace backend_api.Api.Models;

public partial class Portfolio
{
    public string UserId { get; set; } = null!;

    public string Symbol { get; set; } = null!;

    public int TotalQuantity { get; set; }

    public int LockedQuantity { get; set; }

    /// <summary>Computed SQL column: TotalQuantity − LockedQuantity.</summary>
    public int AvailableQuantity { get; set; }

    public decimal AvgCostPrice { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual Symbol SymbolNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
