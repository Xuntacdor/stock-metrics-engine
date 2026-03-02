namespace backend_api.Api.DTOs;

public class CreateCorporateActionRequest
{
    public string Symbol { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public DateTime RecordDate { get; set; }

    public DateTime PaymentDate { get; set; }
   
    public decimal Ratio { get; set; }

    public string? Note { get; set; }
}

public class UpdateCorporateActionRequest
{
    public DateTime? RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public decimal? Ratio { get; set; }
    public string? Status { get; set; }  
    public string? Note { get; set; }
}

public class CorporateActionResponse
{
    public int ActionId { get; set; }
    public string Symbol { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public DateTime RecordDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Ratio { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? ProcessedAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
