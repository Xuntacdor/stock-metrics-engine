using backend_api.Api.DTOs;
using backend_api.Api.Repositories;

namespace backend_api.Api.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepo;

    public TransactionService(ITransactionRepository transactionRepo)
    {
        _transactionRepo = transactionRepo;
    }

    public async Task<IEnumerable<TransactionResponse>> GetMyTransactionsAsync(string userId, string? transType = null)
    {
        var transactions = await _transactionRepo.GetByUserIdAsync(userId, transType);

        return transactions.Select(t => new TransactionResponse
        {
            TransId = t.TransId,
            RefId = t.RefId,
            TransType = t.TransType,
            Amount = t.Amount,
            BalanceBefore = t.BalanceBefore,
            BalanceAfter = t.BalanceAfter,
            Description = t.Description,
            TransTime = t.TransTime
        });
    }
}
