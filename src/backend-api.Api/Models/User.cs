using System;
using System.Collections.Generic;

namespace backend_api.Api.Models;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CashWallet? CashWallet { get; set; }

    public string? RefreshToken { get; set; }
    
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string KycStatus { get; set; } = "PENDING";

    public string AccountStatus { get; set; } = "INACTIVE";

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<KycDocument> KycDocuments { get; set; } = new List<KycDocument>();

    public virtual ICollection<DepositRequest> DepositRequests { get; set; } = new List<DepositRequest>();
}
