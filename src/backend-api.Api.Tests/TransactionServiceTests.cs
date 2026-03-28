using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="TransactionService"/>.
/// The service is a thin projection layer over <see cref="ITransactionRepository"/>;
/// tests verify delegation and the shape of the mapped DTOs.
/// </summary>
public class TransactionServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (TransactionService svc, Mock<ITransactionRepository> repoMock) Build()
    {
        var repoMock = new Mock<ITransactionRepository>();
        var svc      = new TransactionService(repoMock.Object);
        return (svc, repoMock);
    }

    private static List<Transaction> SampleTransactions(string userId) => new()
    {
        new Transaction
        {
            TransId       = 1,
            RefId         = "REF-001",
            UserId        = userId,
            TransType     = "DEPOSIT",
            Amount        = 2_000_000m,
            BalanceBefore = 0m,
            BalanceAfter  = 2_000_000m,
            Description   = "Deposit 2M",
            TransTime     = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)
        },
        new Transaction
        {
            TransId       = 2,
            RefId         = "REF-002",
            UserId        = userId,
            TransType     = "BUY",
            Amount        = -500_000m,
            BalanceBefore = 2_000_000m,
            BalanceAfter  = 1_500_000m,
            Description   = "Buy FPT",
            TransTime     = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc)
        },
        new Transaction
        {
            TransId       = 3,
            RefId         = "REF-003",
            UserId        = userId,
            TransType     = "WITHDRAW",
            Amount        = -300_000m,
            BalanceBefore = 1_500_000m,
            BalanceAfter  = 1_200_000m,
            Description   = "Withdrawal",
            TransTime     = new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc)
        }
    };

    // ── GetMyTransactionsAsync — no filter ────────────────────────────────────

    [Fact]
    public async Task GetMyTransactionsAsync_NoTransTypeFilter_ReturnsAllTransactions()
    {
        var (svc, repo) = Build();
        var data = SampleTransactions("user1");

        repo.Setup(r => r.GetByUserIdAsync("user1", null))
            .ReturnsAsync(data);

        var result = (await svc.GetMyTransactionsAsync("user1")).ToList();

        Assert.Equal(3, result.Count);
        repo.Verify(r => r.GetByUserIdAsync("user1", null), Times.Once);
    }

    [Fact]
    public async Task GetMyTransactionsAsync_NoTransTypeFilter_MapsAllFieldsCorrectly()
    {
        var (svc, repo) = Build();
        var data = SampleTransactions("user1");

        repo.Setup(r => r.GetByUserIdAsync("user1", null)).ReturnsAsync(data);

        var result = (await svc.GetMyTransactionsAsync("user1")).First();

        Assert.Equal(1,            result.TransId);
        Assert.Equal("REF-001",    result.RefId);
        Assert.Equal("DEPOSIT",    result.TransType);
        Assert.Equal(2_000_000m,   result.Amount);
        Assert.Equal(0m,           result.BalanceBefore);
        Assert.Equal(2_000_000m,   result.BalanceAfter);
        Assert.Equal("Deposit 2M", result.Description);
        Assert.NotNull(result.TransTime);
    }

    // ── GetMyTransactionsAsync — with transType filter ────────────────────────

    [Fact]
    public async Task GetMyTransactionsAsync_TransTypeFilter_DelegatesToRepoWithFilter()
    {
        var (svc, repo) = Build();
        var buyOnly = SampleTransactions("user1")
            .Where(t => t.TransType == "BUY")
            .ToList();

        repo.Setup(r => r.GetByUserIdAsync("user1", "BUY"))
            .ReturnsAsync(buyOnly);

        var result = (await svc.GetMyTransactionsAsync("user1", "BUY")).ToList();

        Assert.Single(result);
        Assert.Equal("BUY", result[0].TransType);
        repo.Verify(r => r.GetByUserIdAsync("user1", "BUY"), Times.Once);
    }

    [Fact]
    public async Task GetMyTransactionsAsync_TransTypeFilter_DoesNotPassNullWhenProvided()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.GetByUserIdAsync("user1", "WITHDRAW"))
            .ReturnsAsync(new List<Transaction>())
            .Verifiable();

        await svc.GetMyTransactionsAsync("user1", "WITHDRAW");

        // Must pass the specific filter value, not null
        repo.Verify(r => r.GetByUserIdAsync("user1", "WITHDRAW"), Times.Once);
        repo.Verify(r => r.GetByUserIdAsync("user1", null), Times.Never);
    }

    [Fact]
    public async Task GetMyTransactionsAsync_NoTransactions_ReturnsEmptyEnumerable()
    {
        var (svc, repo) = Build();

        repo.Setup(r => r.GetByUserIdAsync("emptyUser", null))
            .ReturnsAsync(new List<Transaction>());

        var result = await svc.GetMyTransactionsAsync("emptyUser");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMyTransactionsAsync_ReturnsCorrectNumberOfMappedItems()
    {
        var (svc, repo) = Build();
        var data = SampleTransactions("user1");

        repo.Setup(r => r.GetByUserIdAsync("user1", null)).ReturnsAsync(data);

        var result = await svc.GetMyTransactionsAsync("user1");

        // The service must return one TransactionResponse per Transaction
        Assert.Equal(data.Count, result.Count());
    }
}
