using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly QuantIQContext _context;

    public TransactionRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, string? transType = null)
    {
        var query = _context.Transactions.Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(transType))
            query = query.Where(t => t.TransType == transType.ToUpper());

        return await query.OrderByDescending(t => t.TransTime).ToListAsync();
    }

    public async Task AddAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
