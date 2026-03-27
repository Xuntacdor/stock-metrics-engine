using System.Security.Claims;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IPriceAlertRepository _repo;
    private readonly ILogger<AlertsController> _logger;

    private static readonly HashSet<string> ValidTypes      = ["price", "volume", "rsi", "news"];
    private static readonly HashSet<string> ValidConditions = ["gt", "gte", "lt", "lte"];

    public AlertsController(IPriceAlertRepository repo, ILogger<AlertsController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>GET /api/alerts — list caller's alerts</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyAlerts()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var alerts = await _repo.GetByUserIdAsync(userId);
        return Ok(alerts.Select(Map));
    }

    /// <summary>POST /api/alerts — create a new alert</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (!ValidTypes.Contains(request.AlertType))
            return BadRequest(new { message = $"Invalid alertType. Allowed: {string.Join(", ", ValidTypes)}" });

        if (!ValidConditions.Contains(request.Condition))
            return BadRequest(new { message = $"Invalid condition. Allowed: {string.Join(", ", ValidConditions)}" });

        var alert = new PriceAlert
        {
            UserId         = userId,
            Symbol         = request.Symbol.ToUpper().Trim(),
            AlertType      = request.AlertType,
            Condition      = request.Condition,
            ThresholdValue = request.ThresholdValue,
            NotifyOnce     = request.NotifyOnce,
            IsActive       = true,
            IsTriggered    = false,
            CreatedAt      = DateTime.UtcNow
        };

        await _repo.AddAsync(alert);
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Alert #{AlertId} created by user {UserId} for {Symbol}", alert.AlertId, userId, alert.Symbol);
        return CreatedAtAction(nameof(GetMyAlerts), Map(alert));
    }

    /// <summary>PUT /api/alerts/{id} — toggle IsActive</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Toggle(int id, [FromBody] UpdateAlertRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var alert = await _repo.GetByIdAsync(id);
        if (alert == null || alert.UserId != userId) return NotFound();

        alert.IsActive = request.IsActive;
        _repo.Update(alert);
        await _repo.SaveChangesAsync();

        return Ok(Map(alert));
    }

    /// <summary>DELETE /api/alerts/{id}</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var alert = await _repo.GetByIdAsync(id);
        if (alert == null || alert.UserId != userId) return NotFound();

        _repo.Delete(alert);
        await _repo.SaveChangesAsync();

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AlertResponse Map(PriceAlert a) => new(
        a.AlertId, a.Symbol, a.AlertType, a.Condition,
        a.ThresholdValue, a.IsActive, a.IsTriggered,
        a.NotifyOnce, a.CreatedAt, a.TriggeredAt);

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? User.FindFirstValue("userId");
}
