using FunkoApi.Models;

namespace FunkoApi.Repository;

public interface IFunkoRepository
{
    Task<Funko?> GetByIdAsync(long id);
    Task<List<Funko>> GetAllAsync();
    Task<Funko> CreateAsync(Funko funko);
    Task<Funko?> UpdateAsync(long id, Funko funko);
    Task<Funko?> DeleteAsync(long id);
    
}