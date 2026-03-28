using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;

namespace backend_api.Api.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepo;
    private readonly ITransactionRepository _transactionRepo;

    public WalletService(IWalletRepository walletRepo, ITransactionRepository transactionRepo)
    {
        _walletRepo = walletRepo;
        _transactionRepo = transactionRepo;
    }

    public async Task<WalletResponse> GetMyWalletAsync(string userId)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            return new WalletResponse { Balance = 0, LockedAmount = 0, AvailableBalance = 0 };
        }

        return MapToResponse(wallet);
    }

    public async Task<WalletResponse> DepositAsync(string userId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be greater than 0.");

        var wallet = await _walletRepo.GetByUserIdAsync(userId);

        if (wallet == null)
        {
            wallet = new CashWallet
            {
                UserId = userId,
                Balance = amount,
                LockedAmount = 0,
                LastUpdated = DateTime.UtcNow
            };
            await _walletRepo.AddAsync(wallet);
            await _walletRepo.SaveChangesAsync();
        }
        else
        {
            var balanceBefore = wallet.Balance;
            wallet.Balance = balanceBefore + amount;
            wallet.LastUpdated = DateTime.UtcNow;
            _walletRepo.Update(wallet);
            await _walletRepo.SaveChangesAsync();

            await RecordTransactionAsync(userId, "DEPOSIT", amount, balanceBefore, wallet.Balance,
                $"Deposit: {amount:N0} VNĐ");
        }

        return MapToResponse(wallet);
    }

    public async Task<WalletResponse> WithdrawAsync(string userId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdraw amount must be greater than 0.");

        var wallet = await _walletRepo.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Wallet not found.");

        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {wallet.AvailableBalance:N0} VNĐ, Required: {amount:N0} VNĐ.");

        var balanceBefore = wallet.Balance;
        wallet.Balance -= amount;
        wallet.LastUpdated = DateTime.UtcNow;
        _walletRepo.Update(wallet);
        await _walletRepo.SaveChangesAsync();

        await RecordTransactionAsync(userId, "WITHDRAW", -amount, balanceBefore, wallet.Balance,
            $"Withdraw: {amount:N0} VNĐ");

        return MapToResponse(wallet);
    }

    private async Task RecordTransactionAsync(
        string userId, string transType, decimal amount,
        decimal balanceBefore, decimal balanceAfter, string description)
    {
        var tx = new Transaction
        {
            UserId = userId,
            RefId = Guid.NewGuid().ToString(),
            TransType = transType,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Description = description,
            TransTime = DateTime.UtcNow
        };
        await _transactionRepo.AddAsync(tx);
        await _transactionRepo.SaveChangesAsync();
    }

    private static WalletResponse MapToResponse(CashWallet w) => new()
    {
        Balance          = w.Balance,
        LockedAmount     = w.LockedAmount,
        AvailableBalance = w.AvailableBalance,
        LastUpdated      = w.LastUpdated
    };
}
