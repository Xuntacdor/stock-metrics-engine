using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface IPortfolioService
{
    Task<PortfolioSummaryResponse> GetMyPortfolioAsync(string userId);
}
