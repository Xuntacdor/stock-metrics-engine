using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface ICorporateActionRepository
{
    Task<IEnumerable<CorporateAction>> GetAllAsync();
    Task<IEnumerable<CorporateAction>> GetBySymbolAsync(string symbol);
    Task<CorporateAction?> GetByIdAsync(int actionId);

    /// <summary>
    /// Lấy các sự kiện đến hạn trả (PaymentDate = today) và chưa xử lý (Status = PENDING).
    /// Được gọi bởi Background Worker mỗi ngày.
    /// </summary>
    Task<IEnumerable<CorporateAction>> GetPendingForTodayAsync(DateTime today);

    Task AddAsync(CorporateAction action);
    void Update(CorporateAction action);
    Task SaveChangesAsync();
}
