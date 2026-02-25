namespace backend_api.Api.DTOs;

public class TransactionResponse
{
    public long TransId { get; set; }
    public string RefId { get; set; } = null!;
    public string TransType { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public DateTime? TransTime { get; set; }
}
