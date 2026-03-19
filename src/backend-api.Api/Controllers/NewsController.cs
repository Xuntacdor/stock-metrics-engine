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
}
