using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IWalletService
{
    Task<WalletResponse> GetMyWalletAsync(string userId);
    Task<WalletResponse> DepositAsync(string userId, decimal amount);
    Task<WalletResponse> WithdrawAsync(string userId, decimal amount);
}
