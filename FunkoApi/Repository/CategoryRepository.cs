using FunkoApi.DataBase;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repository;

public class CategoryRepository(Context dataBaseContext) : ICategoryRepository
{
    public async Task<Category?> GetByNameAsync(string name)
    {
        var foundCategory = await dataBaseContext.Categories
            .FirstOrDefaultAsync(c => c.Nombre.ToLower() == name.ToLower());
        
        return foundCategory;
    }
    
    public async Task<Category?> GetByIdAsync(Guid id) {
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);
        return foundCategory;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await dataBaseContext.Categories.ToListAsync();
    }

    public async Task<Category> CreateAsync(Category category)
    {
        var savedCategory = await dataBaseContext.Categories.AddAsync(category);
        await dataBaseContext.SaveChangesAsync();
        return savedCategory.Entity;
    }

    public async Task<Category?> UpdateAsync(Guid id, Category category)
    {
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);

        if (foundCategory != null)
        {
            foundCategory.Nombre = category.Nombre;
            foundCategory.UpdatedAt = DateTime.UtcNow;
            await dataBaseContext.SaveChangesAsync();
            return foundCategory;
        }
        
        return null;
    }

    public async Task<Category?> DeleteAsync(Guid id)
    {
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);

        if (foundCategory != null)
        {
            dataBaseContext.Categories.Remove(foundCategory);
            await dataBaseContext.SaveChangesAsync();
            return foundCategory;
        }
        return null;
    }
}
