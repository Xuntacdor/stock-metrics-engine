namespace backend_api.Api.DTOs;

public class WalletResponse
{
    public decimal Balance { get; set; }
    public decimal LockedAmount { get; set; }
    public decimal AvailableBalance { get; set; }
    public DateTime? LastUpdated { get; set; }
}

public class DepositWithdrawRequest
{
    public decimal Amount { get; set; }
}
