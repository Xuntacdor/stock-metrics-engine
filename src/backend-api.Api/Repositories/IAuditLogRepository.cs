namespace backend_api.Api.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(Models.AuditLog log);
    Task<IEnumerable<Models.AuditLog>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Models.AuditLog>> GetAllPagedAsync(int page, int pageSize);
    Task SaveChangesAsync();
}
