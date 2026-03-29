using backend_api.Api.DTOs;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Controllers;

[Route("api/symbols")]
[ApiController]
[Authorize] 
public class SymbolsController : ControllerBase
{
    private readonly ISymbolService _symbolService;

    public SymbolsController(ISymbolService symbolService)
    {
        _symbolService = symbolService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var symbols = await _symbolService.GetAllSymbolsAsync();
        return Ok(symbols);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSymbolRequest request)
    {
        try
        {
            var result = await _symbolService.CreateSymbolAsync(request);
            return CreatedAtAction(nameof(GetAll), new { }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{symbol}")]
    public async Task<IActionResult> Delete(string symbol)
    {
        try
        {
            await _symbolService.DeleteSymbolAsync(symbol);
            return Ok(new { message = $"Deleted symbol {symbol} successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch(DbUpdateException)
        {
            return BadRequest(new { message = $"Can't delete symbol {symbol} because it has related data." });
        }
    }

    /// <summary>GET /api/symbols/{symbol}/candles?limit=200 — Redis-cached for 30 s</summary>
    [HttpGet("{symbol}/candles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCandles(string symbol, [FromQuery] int limit = 200)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest(new { message = "Symbol is required." });

        var candles = await _symbolService.GetCandlesAsync(symbol, limit);
        return Ok(candles);
    }
}