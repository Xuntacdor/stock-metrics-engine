using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[Route("api/admin/audit-logs")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Get all audit logs (paginated).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var logs = await _auditLogService.GetAllLogsPagedAsync(page, pageSize);
        return Ok(logs);
    }

    /// <summary>
    /// Get audit logs for a specific user.
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var logs = await _auditLogService.GetLogsByUserAsync(userId);
        return Ok(logs);
    }
}
