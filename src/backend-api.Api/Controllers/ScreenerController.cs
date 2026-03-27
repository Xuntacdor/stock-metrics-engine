using backend_api.Api.DTOs;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScreenerController : ControllerBase
{
    private readonly IScreenerService _screener;

    public ScreenerController(IScreenerService screener) => _screener = screener;

    /// <summary>
    /// GET /api/screener?priceMin=10&amp;priceMax=100&amp;rsiMin=0&amp;rsiMax=30&amp;sortBy=rsi&amp;sortDesc=false
    /// Returns stocks matching the given technical and fundamental filters.
    /// RSI-14 is computed live from candle data stored in the database.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Filter([FromQuery] ScreenerFilterRequest filter)
    {
        if (filter.PriceMin < 0 || filter.RsiMin < 0 || filter.RsiMax > 100)
            return BadRequest(new { message = "Invalid filter range." });

        var results = await _screener.FilterAsync(filter);
        return Ok(results);
    }
}
