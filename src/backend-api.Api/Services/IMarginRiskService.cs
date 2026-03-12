namespace backend_api.Api.Services;

public interface IMarginRiskService
{
 
    Task<decimal> GetBuyingPowerAsync(string userId);

    
    Task<decimal> CalculateRttAsync(string userId);

   
    Task<bool> ValidatePreTradeAsync(string userId, string symbol, int quantity, decimal price);

    
    Task ExecuteForceSellAsync(string userId);
}
