using backend_api.Api.DTOs;
using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IKycRepository
{
    /// <summary>Lấy tất cả bản KYC của một user (mới nhất trước).</summary>
    Task<IEnumerable<KycDocument>> GetByUserIdAsync(string userId);

    /// <summary>Lấy toàn bộ KYC đang PENDING để Admin duyệt.</summary>
    Task<IEnumerable<KycDocument>> GetPendingAsync();

    /// <summary>Lấy chi tiết một bản KYC theo ID.</summary>
    Task<KycDocument?> GetByIdAsync(int kycId);

    /// <summary>Lưu một bản KYC mới vào DB.</summary>
    Task AddAsync(KycDocument document);

    /// <summary>Cập nhật bản KYC (sau khi Admin review).</summary>
    Task UpdateAsync(KycDocument document);

    Task SaveChangesAsync();
}
