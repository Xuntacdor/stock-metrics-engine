using backend_api.Api.Data;
using backend_api.Api.DTOs;
using backend_api.Api.Models;
using backend_api.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Services;

public class SymbolService : ISymbolService
{
    private readonly ISymbolRepository _symbolRepo;
    private readonly ICacheService _cache;
    private readonly ILogger<SymbolService> _logger;
    private readonly QuantIQContext _context;

    private const string AllSymbolsCacheKey = "symbols:all";
    private static readonly TimeSpan SymbolCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CandleCacheTtl = TimeSpan.FromSeconds(30);

    public SymbolService(ISymbolRepository symbolRepo, ICacheService cache, ILogger<SymbolService> logger, QuantIQContext context)
    {
        _symbolRepo = symbolRepo;
        _cache = cache;
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<SymbolDto>> GetAllSymbolsAsync()
    {
        var cached = await _cache.GetAsync<List<SymbolDto>>(AllSymbolsCacheKey);
        if (cached != null)
        {
            _logger.LogDebug("[Cache HIT] {Key}", AllSymbolsCacheKey);
            return cached;
        }

        _logger.LogDebug("[Cache MISS] {Key} — querying DB", AllSymbolsCacheKey);
        var symbols = await _symbolRepo.GetAllAsync();
        var result = symbols.Select(s => new SymbolDto
        {
            Symbol = s.Symbol1,
            CompanyName = s.CompanyName,
            Exchange = s.Exchange
        }).ToList();

        await _cache.SetAsync(AllSymbolsCacheKey, result, SymbolCacheTtl);
        return result;
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

        // Invalidate cache so next read fetches fresh data
        await _cache.RemoveAsync(AllSymbolsCacheKey);

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

        // Invalidate cache
        await _cache.RemoveAsync(AllSymbolsCacheKey);
    }

    public async Task<List<CandleDto>> GetCandlesAsync(string symbol, int limit = 200)
    {
        var sym = symbol.ToUpper().Trim();
        var cacheKey = $"candles:{sym}:{limit}";

        var cached = await _cache.GetAsync<List<CandleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("[Cache HIT] {Key}", cacheKey);
            return cached;
        }

        _logger.LogDebug("[Cache MISS] {Key} — querying DB", cacheKey);

        var candles = await _context.Candles
            .Where(c => c.Symbol == sym)
            .OrderByDescending(c => c.Timestamp)
            .Take(Math.Min(limit, 500))
            .Select(c => new CandleDto
            {
                Timestamp = c.Timestamp,
                Open      = c.Open,
                High      = c.High,
                Low       = c.Low,
                Close     = c.Close,
                Volume    = c.Volume
            })
            .ToListAsync();

        // Return in ascending order for chart rendering
        candles = candles.OrderBy(c => c.Timestamp).ToList();

        await _cache.SetAsync(cacheKey, candles, CandleCacheTtl);
        return candles;
    }
}