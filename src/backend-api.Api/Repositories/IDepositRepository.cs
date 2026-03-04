using backend_api.Api.Models;

namespace backend_api.Api.Repositories;

public interface IDepositRepository
{
   
    Task AddAsync(DepositRequest deposit);

   
    Task<DepositRequest?> GetByOrderCodeAsync(long orderCode);

    
    Task<DepositRequest?> GetByIdAsync(long depositId);

 
    Task<IEnumerable<DepositRequest>> GetByUserIdAsync(string userId);

    
    Task UpdateAsync(DepositRequest deposit);

    Task SaveChangesAsync();
}
