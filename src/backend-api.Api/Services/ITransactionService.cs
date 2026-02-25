using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface ITransactionService
{
    Task<IEnumerable<TransactionResponse>> GetMyTransactionsAsync(string userId, string? transType = null);
}
