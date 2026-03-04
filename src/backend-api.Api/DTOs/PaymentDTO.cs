namespace backend_api.Api.DTOs;


public class CreateDepositRequest
{
    
    public decimal Amount { get; set; }

    
    public string ReturnUrl { get; set; } = null!;

    public string CancelUrl { get; set; } = null!;
}


public class CreateDepositResponse
{
    public long DepositId { get; set; }
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }

   
    public string CheckoutUrl { get; set; } = null!;

    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; }
}

public class DepositDetailResponse
{
    public long DepositId { get; set; }
    public string UserId { get; set; } = null!;
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? CheckoutUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
