using backend_api.Api.DTOs;

namespace backend_api.Api.Services;

public interface ISymbolService
{
    Task<IEnumerable<SymbolDto>> GetAllSymbolsAsync();
    Task<SymbolDto> CreateSymbolAsync(CreateSymbolRequest request);
    Task DeleteSymbolAsync(string symbol);

    /// <summary>
    /// Returns candle data for a symbol, cached in Redis for 30 seconds.
    /// </summary>
    Task<List<CandleDto>> GetCandlesAsync(string symbol, int limit = 200);
}