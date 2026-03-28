using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using Moq;
using Xunit;

namespace backend_api.Api.Tests;

/// <summary>
/// Unit tests for <see cref="WalletService"/>.
/// All repository dependencies are mocked. CashWallet.AvailableBalance is a
/// computed column in SQL Server — it must be set manually in test data because
/// InMemory EF does not evaluate computed column SQL.
/// </summary>
public class WalletServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (
        WalletService svc,
        Mock<IWalletRepository> walletRepo,
        Mock<ITransactionRepository> txRepo)
    Build()
    {
        var walletRepo = new Mock<IWalletRepository>();
        var txRepo     = new Mock<ITransactionRepository>();
        var svc        = new WalletService(walletRepo.Object, txRepo.Object);
        return (svc, walletRepo, txRepo);
    }

    private static CashWallet MakeWallet(
        string userId        = "user1",
        decimal balance      = 5_000_000m,
        decimal lockedAmount = 0m) => new()
    {
        WalletId         = 1,
        UserId           = userId,
        Balance          = balance,
        LockedAmount     = lockedAmount,
        AvailableBalance = balance - lockedAmount,   // simulate computed column
        RowVersion       = new byte[8]
    };

    // ── GetMyWalletAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyWalletAsync_WalletExists_ReturnsCorrectBalances()
    {
        var (svc, walletRepo, _) = Build();
        var wallet = MakeWallet(balance: 2_000_000m, lockedAmount: 500_000m);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1"))
                  .ReturnsAsync(wallet);

        var result = await svc.GetMyWalletAsync("user1");

        Assert.Equal(2_000_000m, result.Balance);
        Assert.Equal(500_000m,   result.LockedAmount);
        Assert.Equal(1_500_000m, result.AvailableBalance);
    }

    [Fact]
    public async Task GetMyWalletAsync_WalletNotFound_ReturnsZeroBalanceResponse()
    {
        var (svc, walletRepo, _) = Build();

        walletRepo.Setup(r => r.GetByUserIdAsync("ghost"))
                  .ReturnsAsync((CashWallet?)null);

        var result = await svc.GetMyWalletAsync("ghost");

        Assert.Equal(0m, result.Balance);
        Assert.Equal(0m, result.LockedAmount);
        Assert.Equal(0m, result.AvailableBalance);
    }

    // ── DepositAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DepositAsync_WalletDoesNotExist_CreatesNewWallet()
    {
        var (svc, walletRepo, txRepo) = Build();

        walletRepo.Setup(r => r.GetByUserIdAsync("newUser"))
                  .ReturnsAsync((CashWallet?)null);
        walletRepo.Setup(r => r.AddAsync(It.IsAny<CashWallet>()))
                  .Returns(Task.CompletedTask)
                  .Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync())
                  .Returns(Task.CompletedTask);

        var result = await svc.DepositAsync("newUser", 1_000_000m);

        walletRepo.Verify(r => r.AddAsync(It.Is<CashWallet>(
            w => w.UserId == "newUser" && w.Balance == 1_000_000m)), Times.Once);

        Assert.Equal(1_000_000m, result.Balance);
    }

    [Fact]
    public async Task DepositAsync_WalletDoesNotExist_DoesNotRecordTransaction()
    {
        // Per implementation: when wallet is null, a new wallet is created but
        // no transaction record is written (that path only happens on update).
        var (svc, walletRepo, txRepo) = Build();

        walletRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
                  .ReturnsAsync((CashWallet?)null);
        walletRepo.Setup(r => r.AddAsync(It.IsAny<CashWallet>())).Returns(Task.CompletedTask);
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await svc.DepositAsync("newUser", 500_000m);

        txRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task DepositAsync_WalletExists_UpdatesBalanceAndRecordsTransaction()
    {
        var (svc, walletRepo, txRepo) = Build();
        var wallet = MakeWallet(balance: 1_000_000m);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>())).Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await svc.DepositAsync("user1", 500_000m);

        Assert.Equal(1_500_000m, result.Balance);
        walletRepo.Verify(r => r.Update(It.Is<CashWallet>(w => w.Balance == 1_500_000m)), Times.Once);
        txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
            t => t.TransType == "DEPOSIT" && t.Amount == 500_000m)), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_ZeroAmount_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => svc.DepositAsync("user1", 0m));
    }

    [Fact]
    public async Task DepositAsync_NegativeAmount_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => svc.DepositAsync("user1", -100m));
    }

    // ── WithdrawAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task WithdrawAsync_SufficientBalance_DeductsBalanceAndRecordsTransaction()
    {
        var (svc, walletRepo, txRepo) = Build();
        var wallet = MakeWallet(balance: 5_000_000m, lockedAmount: 0m);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>())).Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await svc.WithdrawAsync("user1", 2_000_000m);

        Assert.Equal(3_000_000m, result.Balance);
        walletRepo.Verify(r => r.Update(It.Is<CashWallet>(w => w.Balance == 3_000_000m)), Times.Once);
        txRepo.Verify(r => r.AddAsync(It.Is<Transaction>(
            t => t.TransType == "WITHDRAW" && t.Amount == -2_000_000m)), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientBalance_ThrowsInvalidOperationException()
    {
        var (svc, walletRepo, _) = Build();
        // Available = 1_000_000 (Balance=2_000_000, Locked=1_000_000)
        var wallet = MakeWallet(balance: 2_000_000m, lockedAmount: 1_000_000m);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.WithdrawAsync("user1", 1_500_000m));
    }

    [Fact]
    public async Task WithdrawAsync_WalletNotFound_ThrowsKeyNotFoundException()
    {
        var (svc, walletRepo, _) = Build();

        walletRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<string>()))
                  .ReturnsAsync((CashWallet?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => svc.WithdrawAsync("nobody", 100m));
    }

    [Fact]
    public async Task WithdrawAsync_ZeroAmount_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() => svc.WithdrawAsync("user1", 0m));
    }

    [Fact]
    public async Task WithdrawAsync_ExactAvailableBalance_Succeeds()
    {
        var (svc, walletRepo, txRepo) = Build();
        var wallet = MakeWallet(balance: 1_000_000m, lockedAmount: 0m);

        walletRepo.Setup(r => r.GetByUserIdAsync("user1")).ReturnsAsync(wallet);
        walletRepo.Setup(r => r.Update(It.IsAny<CashWallet>())).Verifiable();
        walletRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        txRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        // Withdraw exactly the available balance — should succeed with 0 balance
        var result = await svc.WithdrawAsync("user1", 1_000_000m);

        Assert.Equal(0m, result.Balance);
    }
}
