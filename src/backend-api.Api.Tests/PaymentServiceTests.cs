using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PayOS;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="PaymentService"/>.
/// HandleWebhookAsync is skipped because <see cref="PayOSClient"/> is a sealed
/// third-party class that cannot be mocked with Moq.
/// All other public methods are covered.
/// </summary>
public class PaymentServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QuantIQContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<QuantIQContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new QuantIQContext(options);
    }

    /// <summary>
    /// Creates a PaymentService with all repository dependencies mocked.
    /// The PayOSClient is constructed with dummy credentials that are never
    /// actually called in the tests that avoid the webhook path.
    /// </summary>
    private static (
        PaymentService svc,
        Mock<IDepositRepository> depositRepo,
        Mock<IWalletRepository> walletRepo,
        Mock<ITransactionRepository> txRepo,
        Mock<IAuditLogService> auditLog,
        QuantIQContext ctx)
    Build()
    {
        var depositRepo = new Mock<IDepositRepository>();
        var walletRepo  = new Mock<IWalletRepository>();
        var txRepo      = new Mock<ITransactionRepository>();
        var auditLog    = new Mock<IAuditLogService>();
        var logger      = NullLogger<PaymentService>.Instance;
        var ctx         = BuildContext();

        // PayOSClient cannot be mocked; use real instance with dummy keys.
        // None of the tested methods invoke the PayOSClient.
        var payOS = new PayOSClient(
            clientId:   "test-client-id",
            apiKey:     "test-api-key",
            checksumKey:"test-checksum-key");

        var svc = new PaymentService(
            depositRepo.Object,
            walletRepo.Object,
            txRepo.Object,
            ctx,
            payOS,
            logger,
            auditLog.Object);

        return (svc, depositRepo, walletRepo, txRepo, auditLog, ctx);
    }

    // ── CreateDepositLinkAsync — validation guards ────────────────────────────

    [Fact]
    public async Task CreateDepositLinkAsync_AmountLessThan1000_ThrowsArgumentException()
    {
        var (svc, _, _, _, _, _) = Build();

        var request = new CreateDepositRequest
        {
            Amount    = 999m,
            ReturnUrl = "https://example.com/return",
            CancelUrl = "https://example.com/cancel"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateDepositLinkAsync("user1", request));
    }

    [Fact]
    public async Task CreateDepositLinkAsync_ExactlyMinimumAmount999_ThrowsArgumentException()
    {
        var (svc, _, _, _, _, _) = Build();

        var request = new CreateDepositRequest
        {
            Amount    = 999m,
            ReturnUrl = "https://example.com/return",
            CancelUrl = "https://example.com/cancel"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateDepositLinkAsync("user1", request));
    }

    [Fact]
    public async Task CreateDepositLinkAsync_MissingReturnUrl_ThrowsArgumentException()
    {
        var (svc, _, _, _, _, _) = Build();

        var request = new CreateDepositRequest
        {
            Amount    = 50_000m,
            ReturnUrl = "",                           // empty
            CancelUrl = "https://example.com/cancel"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateDepositLinkAsync("user1", request));
    }

    [Fact]
    public async Task CreateDepositLinkAsync_MissingCancelUrl_ThrowsArgumentException()
    {
        var (svc, _, _, _, _, _) = Build();

        var request = new CreateDepositRequest
        {
            Amount    = 50_000m,
            ReturnUrl = "https://example.com/return",
            CancelUrl = "   "                         // whitespace only
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateDepositLinkAsync("user1", request));
    }

    [Fact]
    public async Task CreateDepositLinkAsync_BothUrlsMissing_ThrowsArgumentException()
    {
        var (svc, _, _, _, _, _) = Build();

        var request = new CreateDepositRequest
        {
            Amount    = 100_000m,
            ReturnUrl = null!,
            CancelUrl = null!
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.CreateDepositLinkAsync("user1", request));
    }

    // ── CancelDepositAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelDepositAsync_DepositNotFound_ReturnsSilently()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        depositRepo.Setup(r => r.GetByOrderCodeAsync(12345L))
                   .ReturnsAsync((DepositRequest?)null);

        // Must not throw when deposit is not found
        var exception = await Record.ExceptionAsync(
            () => svc.CancelDepositAsync(12345L));

        Assert.Null(exception);
        depositRepo.Verify(r => r.UpdateAsync(It.IsAny<DepositRequest>()), Times.Never);
    }

    [Fact]
    public async Task CancelDepositAsync_PendingDeposit_UpdatesStatusToCancelled()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        var deposit = new DepositRequest
        {
            DepositId = 1,
            UserId    = "user1",
            OrderCode = 999L,
            Amount    = 50_000m,
            Status    = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        depositRepo.Setup(r => r.GetByOrderCodeAsync(999L)).ReturnsAsync(deposit);
        depositRepo.Setup(r => r.UpdateAsync(It.IsAny<DepositRequest>()))
                   .Returns(Task.CompletedTask)
                   .Verifiable();
        depositRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.CancelDepositAsync(999L);

        depositRepo.Verify(r => r.UpdateAsync(It.Is<DepositRequest>(
            d => d.Status == "CANCELLED")), Times.Once);
    }

    [Fact]
    public async Task CancelDepositAsync_AlreadyPaidDeposit_DoesNotChangeStatus()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        var deposit = new DepositRequest
        {
            DepositId = 2,
            UserId    = "user1",
            OrderCode = 888L,
            Amount    = 100_000m,
            Status    = "PAID",       // already paid — must not be cancelled
            CreatedAt = DateTime.UtcNow
        };

        depositRepo.Setup(r => r.GetByOrderCodeAsync(888L)).ReturnsAsync(deposit);

        await svc.CancelDepositAsync(888L);

        depositRepo.Verify(r => r.UpdateAsync(It.IsAny<DepositRequest>()), Times.Never);
    }

    // ── GetDepositHistoryAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetDepositHistoryAsync_DelegatesToRepo_ReturnsMappedDtos()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        var deposits = new List<DepositRequest>
        {
            new()
            {
                DepositId   = 1,
                UserId      = "user1",
                OrderCode   = 100L,
                Amount      = 50_000m,
                Status      = "PAID",
                CheckoutUrl = "https://pay.vn/1",
                CreatedAt   = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                PaidAt      = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc)
            },
            new()
            {
                DepositId   = 2,
                UserId      = "user1",
                OrderCode   = 200L,
                Amount      = 100_000m,
                Status      = "PENDING",
                CheckoutUrl = "https://pay.vn/2",
                CreatedAt   = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                PaidAt      = null
            }
        };

        depositRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(deposits);

        var result = (await svc.GetDepositHistoryAsync("user1")).ToList();

        Assert.Equal(2,         result.Count);
        Assert.Equal(1,         result[0].DepositId);
        Assert.Equal(100L,      result[0].OrderCode);
        Assert.Equal(50_000m,   result[0].Amount);
        Assert.Equal("PAID",    result[0].Status);
        Assert.NotNull(result[0].PaidAt);
        Assert.Null(result[1].PaidAt);
    }

    [Fact]
    public async Task GetDepositHistoryAsync_NoDeposits_ReturnsEmptyList()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        depositRepo.Setup(r => r.GetByUserIdAsync("empty"))
                   .ReturnsAsync(new List<DepositRequest>());

        var result = await svc.GetDepositHistoryAsync("empty");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDepositHistoryAsync_DelegatesToRepo_WithCorrectUserId()
    {
        var (svc, depositRepo, _, _, _, _) = Build();

        depositRepo.Setup(r => r.GetByUserIdAsync("user-specific"))
                   .ReturnsAsync(new List<DepositRequest>())
                   .Verifiable();

        await svc.GetDepositHistoryAsync("user-specific");

        depositRepo.Verify(r => r.GetByUserIdAsync("user-specific"), Times.Once);
    }
}
