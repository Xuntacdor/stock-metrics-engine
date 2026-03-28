using backend_api.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardRepository _repo;
    public LeaderboardController(ILeaderboardRepository repo) => _repo = repo;

    /// <summary>GET /api/leaderboard?limit=20 — Top traders ranked by Realized P&amp;L.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 20)
    {
        limit = Math.Min(limit, 100);
        var result = await _repo.GetTopTradersAsync(limit);
        return Ok(result);
    }
}
