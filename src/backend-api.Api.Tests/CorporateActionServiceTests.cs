using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="CorporateActionService"/>.
/// All four repositories are mocked. Tests cover the read delegates,
/// create/update validations, and the three ProcessActionAsync action types.
/// </summary>
public class CorporateActionServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        CorporateActionService svc,
        Mock<ICorporateActionRepository> actionRepo,
        Mock<IPortfolioRepository> portfolioRepo,
        Mock<IWalletRepository> walletRepo,
        Mock<ITransactionRepository> txRepo)
    Build()
    {
        var actionRepo    = new Mock<ICorporateActionRepository>();
        var portfolioRepo = new Mock<IPortfolioRepository>();
        var walletRepo    = new Mock<IWalletRepository>();
        var txRepo        = new Mock<ITransactionRepository>();
        var logger        = NullLogger<CorporateActionService>.Instance;

        var svc = new CorporateActionService(
            actionRepo.Object,
            portfolioRepo.Object,
            walletRepo.Object,
            txRepo.Object,
            logger);

        return (svc, actionRepo, portfolioRepo, walletRepo, txRepo);
    }

    private static CorporateAction MakeAction(
        int id, string symbol, string actionType,
        decimal ratio, string status = "PENDING") => new()
    {
        ActionId    = id,
        Symbol      = symbol,
        ActionType  = actionType,
        RecordDate  = new DateTime(2026, 3, 1),
        PaymentDate = new DateTime(2026, 3, 10),
        Ratio       = ratio,
        Status      = status,
        CreatedAt   = DateTime.UtcNow
    };

    private static Portfolio MakePortfolio(
        string userId, string symbol,
        int totalQty, decimal avgCost) => new()
    {
        UserId            = userId,
        Symbol            = symbol,
        TotalQuantity     = totalQty,
        LockedQuantity    = 0,
        AvailableQuantity = totalQty,
        AvgCostPrice      = avgCost,
        RowVersion        = new byte[8]
    };

    private static CashWallet MakeWallet(string userId, decimal balance) => new()
    {
        WalletId         = 1,
        UserId           = userId,
        Balance          = balance,
        LockedAmount     = 0m,
        AvailableBalance = balance,
        RowVersion       = new byte[8]
    };

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_DelegatesToRepo_ReturnsMappedResponses()
    {
        var (svc, actionRepo, _, _, _) = Build();
        var actions = new List<CorporateAction>
        {
            MakeAction(1, "FPT", "CASH_DIVIDEND", 500m),
            MakeAction(2, "VIC", "STOCK_DIVIDEND", 0.1m)
        };

        actionRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(actions);

        var result = (await svc.GetAllAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("FPT", result[0].Symbol);
        Assert.Equal("VIC", result[1].Symbol);
        actionRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ── GetBySymbolAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetBySymbolAsync_UpperCasesSymbol_BeforeDelegating()
    {
        var (svc, actionRepo, _, _, _) = Build();

        actionRepo.Setup(r => r.GetBySymbolAsync("FPT"))
                  .ReturnsAsync(new List<CorporateAction> { MakeAction(1, "FPT", "CASH_DIVIDEND", 500m) });

        var result = (await svc.GetBySymbolAsync("fpt")).ToList();

        Assert.Single(result);
        actionRepo.Verify(r => r.GetBySymbolAsync("FPT"), Times.Once);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ActionExists_ReturnsMappedResponse()
    {
        var (svc, actionRepo, _, _, _) = Build();
        var action = MakeAction(5, "VIC", "BONUS_SHARE", 0.05m);

        actionRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(action);

        var result = await svc.GetByIdAsync(5);

        Assert.Equal(5,            result.ActionId);
        Assert.Equal("VIC",        result.Symbol);
        Assert.Equal("BONUS_SHARE", result.ActionType);
    }

    [Fact]
    public async Task GetByIdAsync_ActionNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, actionRepo, _, _, _) = Build();

        actionRepo.Setup(r => r.GetByIdAsync(99))
                  .ReturnsAsync((CorporateAction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetByIdAsync(99));
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsMappedAction()
    {
        var (svc, actionRepo, _, _, _) = Build();

        actionRepo.Setup(r => r.AddAsync(It.IsAny<CorporateAction>())).Returns(Task.CompletedTask);
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var request = new CreateCorporateActionRequest
        {
            Symbol      = "fpt",
            ActionType  = "CASH_DIVIDEND",
            RecordDate  = new DateTime(2026, 4, 1),
            PaymentDate = new DateTime(2026, 4, 10),
            Ratio       = 500m,
            Note        = "Annual dividend"
        };

        var result = await svc.CreateAsync(request);

        Assert.Equal("FPT",          result.Symbol);        // uppercased
        Assert.Equal("CASH_DIVIDEND", result.ActionType);
        Assert.Equal("PENDING",       result.Status);
        Assert.Equal(500m,            result.Ratio);
    }

    [Fact]
    public async Task CreateAsync_InvalidActionType_ThrowsArgumentException()
    {
        var (svc, _, _, _, _) = Build();

        var request = new CreateCorporateActionRequest
        {
            Symbol      = "FPT",
            ActionType  = "UNKNOWN_TYPE",
            RecordDate  = new DateTime(2026, 4, 1),
            PaymentDate = new DateTime(2026, 4, 10),
            Ratio       = 1m
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_ZeroRatio_ThrowsArgumentException()
    {
        var (svc, _, _, _, _) = Build();

        var request = new CreateCorporateActionRequest
        {
            Symbol      = "FPT",
            ActionType  = "CASH_DIVIDEND",
            RecordDate  = new DateTime(2026, 4, 1),
            PaymentDate = new DateTime(2026, 4, 10),
            Ratio       = 0m
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_PaymentDateBeforeRecordDate_ThrowsArgumentException()
    {
        var (svc, _, _, _, _) = Build();

        var request = new CreateCorporateActionRequest
        {
            Symbol      = "FPT",
            ActionType  = "CASH_DIVIDEND",
            RecordDate  = new DateTime(2026, 4, 10),
            PaymentDate = new DateTime(2026, 4, 5),   // before RecordDate
            Ratio       = 500m
        };

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(request));
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ActionIsProcessed_ThrowsInvalidOperationException()
    {
        var (svc, actionRepo, _, _, _) = Build();
        var action = MakeAction(1, "FPT", "CASH_DIVIDEND", 500m, status: "PROCESSED");

        actionRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(action);

        var request = new UpdateCorporateActionRequest { Ratio = 600m };

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAsync(1, request));
    }

    [Fact]
    public async Task UpdateAsync_PendingAction_UpdatesFieldsAndSaves()
    {
        var (svc, actionRepo, _, _, _) = Build();
        var action = MakeAction(2, "VIC", "STOCK_DIVIDEND", 0.1m, status: "PENDING");

        actionRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(action);
        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var request = new UpdateCorporateActionRequest { Ratio = 0.2m, Note = "Updated note" };

        var result = await svc.UpdateAsync(2, request);

        Assert.Equal(0.2m,          result.Ratio);
        Assert.Equal("Updated note", result.Note);
        actionRepo.Verify(r => r.Update(It.IsAny<CorporateAction>()), Times.Once);
        actionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatusValue_ThrowsArgumentException()
    {
        var (svc, actionRepo, _, _, _) = Build();
        var action = MakeAction(3, "FPT", "CASH_DIVIDEND", 500m, status: "PENDING");

        actionRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(action);

        var request = new UpdateCorporateActionRequest { Status = "PROCESSED" };  // not allowed via update

        await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateAsync(3, request));
    }

    [Fact]
    public async Task UpdateAsync_ActionNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, actionRepo, _, _, _) = Build();

        actionRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CorporateAction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.UpdateAsync(999, new UpdateCorporateActionRequest()));
    }

    // ── ProcessActionAsync — CASH_DIVIDEND ────────────────────────────────────

    [Fact]
    public async Task ProcessActionAsync_CashDividend_CreditsDividendToWallet()
    {
        // ratio = 500 VND/share, qty = 1000 → dividend = 500_000
        var (svc, actionRepo, portfolioRepo, walletRepo, txRepo) = Build();

        var action  = MakeAction(10, "FPT", "CASH_DIVIDEND", 500m, status: "PENDING");
        var holding = MakePortfolio("user1", "FPT", totalQty: 1000, avgCost: 80_000m);
        var wallet  = MakeWallet("user1", balance: 0m);

        actionRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(action);
        portfolioRepo.Setup(r => r.GetBySymbolAsync("FPT"))
                     .ReturnsAsync(new List<Portfolio> { holding });
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>())).Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.ProcessActionAsync(10);

        walletRepo.Verify(r => r.Update(It.Is<CashWallet>(
            w => w.Balance == 500_000m)), Times.Once);

        txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
            t => t.TransType == "CASH_DIVIDEND" && t.Amount == 500_000m)), Times.Once);
    }

    [Fact]
    public async Task ProcessActionAsync_CashDividend_NoWallet_SkipsUser()
    {
        var (svc, actionRepo, portfolioRepo, walletRepo, txRepo) = Build();

        var action  = MakeAction(11, "FPT", "CASH_DIVIDEND", 500m, status: "PENDING");
        var holding = MakePortfolio("user1", "FPT", totalQty: 100, avgCost: 80_000m);

        actionRepo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(action);
        portfolioRepo.Setup(r => r.GetBySymbolAsync("FPT"))
                     .ReturnsAsync(new List<Portfolio> { holding });
        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
                  .ReturnsAsync((CashWallet?)null);    // no wallet

        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.ProcessActionAsync(11);

        // No wallet credit, no transaction
        walletRepo.Verify(r => r.Update(It.IsAny<CashWallet>()), Times.Never);
        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);

        // Action still marked PROCESSED
        actionRepo.Verify(r => r.Update(It.Is<CorporateAction>(a => a.Status == "PROCESSED")), Times.Once);
    }

    // ── ProcessActionAsync — STOCK_DIVIDEND ────────────────────────────────────

    [Fact]
    public async Task ProcessActionAsync_StockDividend_AddsSharesAndAdjustsCost()
    {
        // qty = 100, ratio = 0.1 → bonusShares = floor(100 * 0.1) = 10
        // newTotalQty = 110, oldCost = 100 * 80_000 = 8_000_000
        // newAvgCost = 8_000_000 / 110 ≈ 72_727.27
        var (svc, actionRepo, portfolioRepo, walletRepo, txRepo) = Build();

        var action  = MakeAction(20, "VIC", "STOCK_DIVIDEND", 0.1m, status: "PENDING");
        var holding = MakePortfolio("user1", "VIC", totalQty: 100, avgCost: 80_000m);

        actionRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(action);
        portfolioRepo.Setup(r => r.GetBySymbolAsync("VIC"))
                     .ReturnsAsync(new List<Portfolio> { holding });
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>())).Verifiable();
        portfolioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.ProcessActionAsync(20);

        portfolioRepo.Verify(r => r.Update(It.Is<Portfolio>(
            p => p.TotalQuantity == 110)), Times.Once);

        txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
            t => t.TransType == "STOCK_DIVIDEND")), Times.Once);
    }

    // ── ProcessActionAsync — BONUS_SHARE ─────────────────────────────────────

    [Fact]
    public async Task ProcessActionAsync_BonusShare_AddsSharesCorrectly()
    {
        // qty = 200, ratio = 0.05 → bonusShares = floor(200 * 0.05) = 10
        var (svc, actionRepo, portfolioRepo, walletRepo, txRepo) = Build();

        var action  = MakeAction(30, "MSN", "BONUS_SHARE", 0.05m, status: "PENDING");
        var holding = MakePortfolio("user1", "MSN", totalQty: 200, avgCost: 50_000m);

        actionRepo.Setup(r => r.GetByIdAsync(30)).ReturnsAsync(action);
        portfolioRepo.Setup(r => r.GetBySymbolAsync("MSN"))
                     .ReturnsAsync(new List<Portfolio> { holding });
        portfolioRepo.Setup(r => r.Update(It.IsAny<Portfolio>())).Verifiable();
        portfolioRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.ProcessActionAsync(30);

        portfolioRepo.Verify(r => r.Update(It.Is<Portfolio>(
            p => p.TotalQuantity == 210)), Times.Once);

        txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
            t => t.TransType == "BONUS_SHARE")), Times.Once);
    }

    // ── ProcessActionAsync — status guards ────────────────────────────────────

    [Fact]
    public async Task ProcessActionAsync_AlreadyProcessed_SkipsWithoutChanges()
    {
        var (svc, actionRepo, portfolioRepo, _, txRepo) = Build();
        var action = MakeAction(40, "FPT", "CASH_DIVIDEND", 500m, status: "PROCESSED");

        actionRepo.Setup(r => r.GetByIdAsync(40)).ReturnsAsync(action);

        await svc.ProcessActionAsync(40);

        // No holdings should be fetched, no changes persisted
        portfolioRepo.Verify(r => r.GetBySymbolAsync(It.IsAny<string>()), Times.Never);
        actionRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ProcessActionAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var (svc, actionRepo, _, _, _) = Build();

        actionRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((CorporateAction?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.ProcessActionAsync(999));
    }

    [Fact]
    public async Task ProcessActionAsync_MarksActionAsProcessed_AfterSuccess()
    {
        var (svc, actionRepo, portfolioRepo, walletRepo, txRepo) = Build();

        var action  = MakeAction(50, "FPT", "CASH_DIVIDEND", 100m, status: "PENDING");
        var holding = MakePortfolio("user1", "FPT", totalQty: 100, avgCost: 80_000m);
        var wallet  = MakeWallet("user1", balance: 0m);

        actionRepo.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(action);
        portfolioRepo.Setup(r => r.GetBySymbolAsync("FPT"))
                     .ReturnsAsync(new List<Portfolio> { holding });
        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>())).Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        actionRepo.Setup(r => r.Update(It.IsAny<CorporateAction>())).Verifiable();
        actionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.ProcessActionAsync(50);

        actionRepo.Verify(r => r.Update(It.Is<CorporateAction>(
            a => a.Status == "PROCESSED" && a.ProcessedAt != null)), Times.Once);
    }
}
