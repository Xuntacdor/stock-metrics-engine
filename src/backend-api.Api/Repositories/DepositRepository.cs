using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class DepositRepository : IDepositRepository
{
    private readonly QuantIQContext _context;

    public DepositRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DepositRequest deposit)
        => await _context.DepositRequests.AddAsync(deposit);

    public async Task<DepositRequest?> GetByOrderCodeAsync(long orderCode)
        => await _context.DepositRequests
            .FirstOrDefaultAsync(d => d.OrderCode == orderCode);

    public async Task<DepositRequest?> GetByIdAsync(long depositId)
        => await _context.DepositRequests
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.DepositId == depositId);

    public async Task<IEnumerable<DepositRequest>> GetByUserIdAsync(string userId)
        => await _context.DepositRequests
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public Task UpdateAsync(DepositRequest deposit)
    {
        _context.DepositRequests.Update(deposit);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
