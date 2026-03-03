using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class KycRepository : IKycRepository
{
    private readonly QuantIQContext _context;

    public KycRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<KycDocument>> GetByUserIdAsync(string userId)
    {
        return await _context.KycDocuments
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<KycDocument>> GetPendingAsync()
    {
        return await _context.KycDocuments
            .Where(k => k.Status == "PENDING")
            .Include(k => k.User)
            .OrderBy(k => k.SubmittedAt)
            .ToListAsync();
    }

    public async Task<KycDocument?> GetByIdAsync(int kycId)
    {
        return await _context.KycDocuments
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.KycId == kycId);
    }

    public async Task AddAsync(KycDocument document)
    {
        await _context.KycDocuments.AddAsync(document);
    }

    public Task UpdateAsync(KycDocument document)
    {
        _context.KycDocuments.Update(document);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
