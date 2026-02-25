using FunkoApi.DTO;
using FunkoApi.Models;

namespace FunkoApi.Repository;

public interface IFunkoRepository
{
    Task<Funko?> GetByIdAsync(long id);
    Task<(IEnumerable<Funko> Items, int TotalCount)> GetAllAsync(FilterDTO filter);    
    Task<Funko> CreateAsync(Funko funko);
    Task<Funko?> UpdateAsync(long id, Funko funko);
    Task<Funko?> DeleteAsync(long id);
    
    //GraphQL
    IQueryable<Funko> FindAllAsNoTracking();

}