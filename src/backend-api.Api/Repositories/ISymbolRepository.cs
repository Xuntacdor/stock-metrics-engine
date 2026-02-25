using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface ISymbolRepository
{
    Task<IEnumerable<Symbol>> GetAllAsync();
    Task<Symbol?> GetByIdAsync(string symbol);
    Task AddAsync(Symbol symbol);
    void Update(Symbol symbol);
    void Delete(Symbol symbol);
    Task SaveChangesAsync();
}