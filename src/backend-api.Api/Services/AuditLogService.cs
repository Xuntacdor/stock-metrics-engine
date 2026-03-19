using System.Text.Json;
using backend_api.Api.Models;
using backend_api.Api.Repositories;

namespace backend_api.Api.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repo;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository repo, ILogger<AuditLogService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task LogAsync(string userId, string action, object? detail = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Detail = detail != null ? JsonSerializer.Serialize(detail) : null,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(log);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("[AuditTrail] User={UserId} Action={Action} IP={IP}", userId, action, ipAddress ?? "N/A");
        }
        catch (Exception ex)
        {
            // Audit logging should never break the main business flow
            _logger.LogError(ex, "[AuditTrail] Failed to write audit log for UserId={UserId} Action={Action}", userId, action);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId)
        => await _repo.GetByUserIdAsync(userId);

    public async Task<IEnumerable<AuditLog>> GetAllLogsPagedAsync(int page, int pageSize)
        => await _repo.GetAllPagedAsync(page, pageSize);
}
