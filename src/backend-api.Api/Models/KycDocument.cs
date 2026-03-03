namespace backend_api.Api.Models;


public class KycDocument
{
    public int KycId { get; set; }

    public string UserId { get; set; } = null!;


    public string? CardNumber { get; set; }

    public string? FullName { get; set; }

    public string? DateOfBirth { get; set; }

    public string? Sex { get; set; }

    public string? Nationality { get; set; }

    public string? HomeTown { get; set; }

    public string? Address { get; set; }

    public string? ExpiryDate { get; set; }

  
    public string? CardType { get; set; }


    public string ImagePath { get; set; } = null!;

  
    public string Status { get; set; } = "PENDING";

    public string? RejectReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public virtual User? User { get; set; }
}
