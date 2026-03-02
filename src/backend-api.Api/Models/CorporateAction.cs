using System;

namespace backend_api.Api.Models;

public partial class CorporateAction
{
    public int ActionId { get; set; }


    public string Symbol { get; set; } = null!;


    public string ActionType { get; set; } = null!;

    public DateTime RecordDate { get; set; }

 
    public DateTime PaymentDate { get; set; }


    public decimal Ratio { get; set; }


    public string Status { get; set; } = "PENDING";

    public DateTime? ProcessedAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Symbol SymbolNavigation { get; set; } = null!;
}
