using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface ICorporateActionService
{
    Task<IEnumerable<CorporateActionResponse>> GetAllAsync();
    Task<IEnumerable<CorporateActionResponse>> GetBySymbolAsync(string symbol);
    Task<CorporateActionResponse> GetByIdAsync(int actionId);
    Task<CorporateActionResponse> CreateAsync(CreateCorporateActionRequest request);
    Task<CorporateActionResponse> UpdateAsync(int actionId, UpdateCorporateActionRequest request);

    
    Task ProcessActionAsync(int actionId);
}
