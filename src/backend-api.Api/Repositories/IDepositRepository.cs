using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IDepositRepository
{
    /// <summary>Tạo mới một phiếu nạp.</summary>
    Task AddAsync(DepositRequest deposit);

    /// <summary>Lấy theo OrderCode — dùng khi nhận webhook (phải UNIQUE).</summary>
    Task<DepositRequest?> GetByOrderCodeAsync(long orderCode);

    /// <summary>Lấy theo ID.</summary>
    Task<DepositRequest?> GetByIdAsync(long depositId);

    /// <summary>Lịch sử nạp tiền của một user (mới nhất trước).</summary>
    Task<IEnumerable<DepositRequest>> GetByUserIdAsync(string userId);

    /// <summary>Cập nhật trạng thái (PAID / CANCELLED).</summary>
    Task UpdateAsync(DepositRequest deposit);

    Task SaveChangesAsync();
}
