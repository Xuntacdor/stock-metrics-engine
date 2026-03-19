namespace backend_api.Api.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Action performed: PlaceOrder, CancelOrder, Deposit, UpdateProfile, etc.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized detail payload of the action.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Client IP address (if available from HttpContext).
    /// </summary>
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
