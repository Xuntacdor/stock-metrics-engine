namespace backend_api.Api.Models;


public class DepositRequest
{
    public long DepositId { get; set; }


    public string UserId { get; set; } = null!;

    
    public long OrderCode { get; set; }


    public decimal Amount { get; set; }

    public string Status { get; set; } = "PENDING";


    public string? CheckoutUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

   
    public virtual User? User { get; set; }
}
