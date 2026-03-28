using backend_api.Api.DTOs;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend_api.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiskController : ControllerBase
{
    private readonly IMarginRiskService _riskService;
    private readonly IRiskAlertRepository _alertRepo;
    private readonly IWalletRepository _walletRepo;
    private readonly IMarginRatioRepository _marginRatioRepo;

    public RiskController(
        IMarginRiskService riskService,
        IRiskAlertRepository alertRepo,
        IWalletRepository walletRepo,
        IMarginRatioRepository marginRatioRepo)
    {
        _riskService = riskService;
        _alertRepo = alertRepo;
        _walletRepo = walletRepo;
        _marginRatioRepo = marginRatioRepo;
    }

    [HttpGet("buying-power")]
    public async Task<IActionResult> GetBuyingPower()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var wallet = await _walletRepo.GetByUserIdAsync(userId);

        if (wallet == null)
            return Ok(new BuyingPowerResponse(0m, 0m, 0m));

        var buyingPower = await _riskService.GetBuyingPowerAsync(userId);
        var availableCash = wallet.AvailableBalance;
        var marginValue = buyingPower - availableCash;

        return Ok(new BuyingPowerResponse(buyingPower, availableCash, marginValue));
    }

    [HttpGet("rtt")]
    public async Task<IActionResult> GetRtt()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var wallet = await _walletRepo.GetByUserIdAsync(userId);

        // User chưa deposit → wallet chưa tồn tại, trả về NO_LOAN thay vì 404
        if (wallet == null)
            return Ok(new RttResponse(999m, 0m, false, "NO_LOAN"));

        var loanAmount = wallet.LoanAmount;
        var rtt = await _riskService.CalculateRttAsync(userId);

        var status = rtt == decimal.MaxValue
            ? "NO_LOAN"
            : rtt < 0.80m ? "FORCE_SELL_ZONE"
            : rtt < 0.85m ? "CALL_MARGIN_ZONE"
            : "SAFE";

        var rttDisplay = rtt == decimal.MaxValue ? 999m : rtt;

        return Ok(new RttResponse(rttDisplay, loanAmount, rtt < 0.85m, status));
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] int limit = 50)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var alerts = await _alertRepo.GetByUserIdAsync(userId, limit);

        var response = alerts.Select(a => new RiskAlertResponse(
            a.AlertId,
            a.AlertType,
            a.Rtt,
            a.Message,
            a.IsAcknowledged,
            a.CreatedAt));

        return Ok(response);
    }

    [HttpPost("simulate-rtt")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SimulateRtt([FromBody] SimulateLoanRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var wallet = await _walletRepo.GetByUserIdAsync(userId);
        if (wallet == null) return NotFound("Wallet not found.");

        wallet.LoanAmount = request.LoanAmount;
        wallet.LastUpdated = DateTime.UtcNow;
        _walletRepo.Update(wallet);
        await _walletRepo.SaveChangesAsync();

        var rtt = await _riskService.CalculateRttAsync(userId);
        return Ok(new { LoanAmount = request.LoanAmount, Rtt = rtt });
    }

    [HttpGet("margin-ratios")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarginRatios()
    {
        var ratios = await _marginRatioRepo.GetAllActiveAsync();
        var response = ratios.Select(r => new MarginRatioResponse(r.Symbol, r.InitialRate, r.MaintenanceRate));
        return Ok(response);
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? User.FindFirstValue("userId");
}
