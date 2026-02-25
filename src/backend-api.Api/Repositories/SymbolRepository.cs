using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Repositories;

public class SymbolRepository : ISymbolRepository
{
    private readonly QuantIQContext _context;

    public SymbolRepository(QuantIQContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Symbol>> GetAllAsync()
    {
        return await _context.Symbols.ToListAsync();
    }

    public async Task<Symbol?> GetByIdAsync(string symbol)
    {
        return await _context.Symbols.FindAsync(symbol);
    }

    public async Task AddAsync(Symbol symbol)
    {
        await _context.Symbols.AddAsync(symbol);
    }

    public void Update(Symbol symbol)
    {
        _context.Symbols.Update(symbol); 
    }

    public void Delete(Symbol symbol)
    {
        _context.Symbols.Remove(symbol);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}