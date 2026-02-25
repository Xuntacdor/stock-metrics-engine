using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface ISymbolService
{
    Task<IEnumerable<SymbolDto>> GetAllSymbolsAsync();
    Task<SymbolDto> CreateSymbolAsync(CreateSymbolRequest request);
    Task DeleteSymbolAsync(string symbol);
}