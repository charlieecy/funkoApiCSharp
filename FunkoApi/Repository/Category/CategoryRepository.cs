using FunkoApi.DataBase;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repository;

public class CategoryRepository(Context dataBaseContext, ILogger<CategoryRepository> logger) : ICategoryRepository
{
    public async Task<Category?> GetByNameAsync(string name)
    {
        logger.LogDebug("Consultando categoría por nombre en BD: {Nombre}", name);
        var foundCategory = await dataBaseContext.Categories
            .FirstOrDefaultAsync(c => c.Nombre.ToLower() == name.ToLower());
        
        return foundCategory;
    }
    
    public async Task<Category?> GetByIdAsync(Guid id) {
        logger.LogDebug("Consultando categoría por id en BD: {Id}", id);
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);
        return foundCategory;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        logger.LogDebug("Consultando todas las categorías en BD");
        var categories = await dataBaseContext.Categories.ToListAsync();
        logger.LogDebug("Categorías obtenidas de BD, total: {Total}", categories.Count);
        return categories;
    }
    
    // OJO, devuelve IQueryable<Category>, NO Task<List<Category>>, porque se usa
    //el AsNoTracking() para ganar velocidad al hacer consultas con GraphQL
    public IQueryable<Category> FindAllAsNoTracking()
    {
        //NO tenemos un .toList()
        return dataBaseContext.Categories.AsNoTracking();
    }

    public async Task<Category> CreateAsync(Category category)
    {
        logger.LogDebug("Guardando nueva categoría en BD: {Nombre}", category.Nombre);
        var savedCategory = await dataBaseContext.Categories.AddAsync(category);
        await dataBaseContext.SaveChangesAsync();
        logger.LogDebug("Categoría guardada en BD con id: {Id}", savedCategory.Entity.Id);
        return savedCategory.Entity;
    }

    public async Task<Category?> UpdateAsync(Guid id, Category category)
    {
        logger.LogDebug("Actualizando categoría en BD con id: {Id}", id);
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);

        if (foundCategory != null)
        {
            foundCategory.Nombre = category.Nombre;
            foundCategory.UpdatedAt = DateTime.UtcNow;
            await dataBaseContext.SaveChangesAsync();
            logger.LogDebug("Categoría con id {Id} actualizada en BD", id);
            return foundCategory;
        }
        
        logger.LogDebug("Categoría con id {Id} no encontrada en BD para actualizar", id);
        return null;
    }

    public async Task<Category?> DeleteAsync(Guid id)
    {
        logger.LogDebug("Eliminando categoría de BD con id: {Id}", id);
        var foundCategory = await dataBaseContext.Categories.FindAsync(id);

        if (foundCategory != null)
        {
            dataBaseContext.Categories.Remove(foundCategory);
            await dataBaseContext.SaveChangesAsync();
            logger.LogDebug("Categoría con id {Id} eliminada de BD", id);
            return foundCategory;
        }
        
        logger.LogDebug("Categoría con id {Id} no encontrada en BD para eliminar", id);
        return null;
    }
}
