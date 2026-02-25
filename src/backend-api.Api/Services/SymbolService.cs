using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;

namespace backend_api.Api.Services;

public class SymbolService : ISymbolService
{
    private readonly ISymbolRepository _symbolRepo;

    public SymbolService(ISymbolRepository symbolRepo)
    {
        _symbolRepo = symbolRepo;
    }

    public async Task<IEnumerable<SymbolDto>> GetAllSymbolsAsync()
    {
        var symbols = await _symbolRepo.GetAllAsync();
        return symbols.Select(s => new SymbolDto
        {
            Symbol = s.Symbol1,
            CompanyName = s.CompanyName,
            Exchange = s.Exchange
        });
    }

    public async Task<SymbolDto> CreateSymbolAsync(CreateSymbolRequest request)
    {
        var symbolCode = request.Symbol.ToUpper().Trim();

        var existingSymbol = await _symbolRepo.GetByIdAsync(symbolCode);
        if (existingSymbol != null)
        {
            throw new InvalidOperationException($"Symbol {symbolCode} already exists in the system.");
        }

        var newSymbol = new Symbol
        {
            Symbol1 = symbolCode, 
            CompanyName = request.CompanyName,
            Exchange = request.Exchange?.ToUpper() 
        };

        await _symbolRepo.AddAsync(newSymbol);
        await _symbolRepo.SaveChangesAsync();

        return new SymbolDto
        {
            Symbol = newSymbol.Symbol1,
            CompanyName = newSymbol.CompanyName,
            Exchange = newSymbol.Exchange
        };
    }

    public async Task DeleteSymbolAsync(string symbol)
    {
        var existingSymbol = await _symbolRepo.GetByIdAsync(symbol.ToUpper());
        if (existingSymbol == null)
            throw new KeyNotFoundException("Symbol not found.");

        _symbolRepo.Delete(existingSymbol);
        await _symbolRepo.SaveChangesAsync();
    }
}