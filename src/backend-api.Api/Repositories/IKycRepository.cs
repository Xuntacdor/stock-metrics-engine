using backend_api.Api.DTOs;
using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IKycRepository
{
    Task<IEnumerable<KycDocument>> GetByUserIdAsync(string userId);

    Task<IEnumerable<KycDocument>> GetPendingAsync();

    Task<KycDocument?> GetByIdAsync(int kycId);

    Task AddAsync(KycDocument document);

    Task UpdateAsync(KycDocument document);

    Task SaveChangesAsync();
}
