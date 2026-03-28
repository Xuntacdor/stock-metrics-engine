using System.Security.Claims;
using backend_api.Api.DTOs;
using backend_api.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly INewsRepository _news;
    public NewsController(INewsRepository news) => _news = news;

    /// <summary>GET /api/news?symbol=FPT&amp;limit=20</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetNews([FromQuery] string? symbol, [FromQuery] int limit = 20)
    {
        var articles = await _news.GetBySymbolAsync(symbol, limit);
        return Ok(articles);
    }

    /// <summary>GET /api/news/sentiment?symbol=FPT&amp;days=7</summary>
    [HttpGet("sentiment")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSentiment([FromQuery] string symbol, [FromQuery] int days = 7)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol is required");
        var summary = await _news.GetSentimentSummaryAsync(symbol, days);
        return Ok(summary);
    }

    /// <summary>
    /// GET /api/news/sentiment/trend?symbol=FPT&amp;days=30
    /// Returns daily aggregated sentiment scores for the given symbol over the past N days.
    /// Each entry contains article counts by label, average model confidence score, and an overall signal.
    /// </summary>
    [HttpGet("sentiment/trend")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSentimentTrend([FromQuery] string symbol, [FromQuery] int days = 30)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol is required");
        days = Math.Clamp(days, 1, 90);
        var trend = await _news.GetSentimentTrendAsync(symbol, days);
        return Ok(trend);
    }
}
