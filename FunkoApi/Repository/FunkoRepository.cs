using System.Linq.Expressions;
using FunkoApi.DataBase;
using FunkoApi.DTO;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repository;

public class FunkoRepository (Context dataBaseContext) : IFunkoRepository
{

    public async Task<Funko?> GetByIdAsync(long id)
    {
        // Usar Include para cargar la relación Category de forma eager loading
        // Esto previene NullReferenceException cuando se accede a funko.Category.Nombre
        var foundFunko = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);
        
        return foundFunko;
    }
    public async Task<(IEnumerable<Funko> Items, int TotalCount)> GetAllAsync(FilterDTO filter)
    {
        var query = dataBaseContext.Funkos.Include(f => f.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(p => EF.Functions.Like(p.Nombre, $"%{filter.Nombre}%"));

        if (!string.IsNullOrWhiteSpace(filter.Categoria))
            query = query.Where(p => EF.Functions.Like(p.Category!.Nombre, $"%{filter.Categoria}%"));

        if (filter.MaxPrecio.HasValue)
            query = query.Where(p => p.Precio <= filter.MaxPrecio.Value);


        var totalCount = await query.CountAsync();
        query = ApplySorting(query, filter.SortBy, filter.Direction);

        var items = await query
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, totalCount);
    }
    
    public async Task<Funko> CreateAsync(Funko funko)
    {
        var savedFunko = await dataBaseContext.Funkos.AddAsync(funko);
        await dataBaseContext.SaveChangesAsync();
        return savedFunko.Entity;
    }

    public async Task<Funko?> UpdateAsync(long id, Funko funko)
    {
        var foundFunko = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (foundFunko != null)
        {
            foundFunko.Id = id;
            foundFunko.Nombre = funko.Nombre;
            foundFunko.Precio = funko.Precio;
            //Solo actualizamos la FK, no hace falta actualizar el campo como tal
            foundFunko.Imagen = funko.Imagen;
            foundFunko.CategoryId = funko.CategoryId;
            foundFunko.UpdatedAt = DateTime.UtcNow;
            await dataBaseContext.SaveChangesAsync();
            return foundFunko;
        }

        return null;
    }
    

    public async Task<Funko?> DeleteAsync(long id)
    {
        // Cambiamos FindAsync por FirstOrDefaultAsync con Include
        var foundFunko = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (foundFunko != null)
        {
            dataBaseContext.Funkos.Remove(foundFunko);
            await dataBaseContext.SaveChangesAsync();
            return foundFunko; // Ahora este objeto sí lleva su Category dentro
        }
    
        return null;
    }
    
    private static IQueryable<Funko> ApplySorting(IQueryable<Funko> query, string sortBy, string direction)
    {
        var isDescending = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        Expression<Func<Funko, object>> keySelector = sortBy.ToLower() switch
        {
            "nombre" => p => p.Nombre,
            "precio" => p => p.Precio,
            "createdat" => p => p.CreatedAt,
            "categoria" => p => p.Category!.Nombre,
            _ => p => p.Id
        };
        return isDescending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
    }
}