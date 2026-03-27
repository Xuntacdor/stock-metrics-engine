namespace backend_api.Api.Models;

public partial class CashWallet
{
    public int WalletId { get; set; }

    public string UserId { get; set; } = null!;

    public decimal Balance { get; set; }

    public decimal LockedAmount { get; set; }

    /// <summary>Computed SQL column: Balance − LockedAmount.</summary>
    public decimal AvailableBalance { get; set; }

    public decimal LoanAmount { get; set; }

    public DateTime? LastUpdated { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
