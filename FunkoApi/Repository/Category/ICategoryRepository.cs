using FunkoApi.Models;

namespace FunkoApi.Repository;

public interface ICategoryRepository
{
    Task<Category?> GetByNameAsync(string name);
    Task<Category?> GetByIdAsync(Guid id);
    Task<List<Category>> GetAllAsync();
    Task<Category> CreateAsync(Category category);
    Task<Category?> UpdateAsync(Guid id, Category category);
    Task<Category?> DeleteAsync(Guid id);
    
    //GraphQL
    IQueryable<Category> FindAllAsNoTracking();
}