namespace backend_api.Api.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Record a sensitive action performed by a user.
    /// </summary>
    /// <param name="userId">ID of the user performing the action.</param>
    /// <param name="action">Action name, e.g. PlaceOrder, CancelOrder, Deposit, UpdateProfile.</param>
    /// <param name="detail">Optional payload object; will be JSON-serialized.</param>
    /// <param name="ipAddress">Optional client IP from HttpContext.</param>
    Task LogAsync(string userId, string action, object? detail = null, string? ipAddress = null);

    Task<IEnumerable<Models.AuditLog>> GetLogsByUserAsync(string userId);
    Task<IEnumerable<Models.AuditLog>> GetAllLogsPagedAsync(int page, int pageSize);
}
