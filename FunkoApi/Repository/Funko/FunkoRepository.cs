using System.Linq.Expressions;
using FunkoApi.DataBase;
using FunkoApi.DTO;
using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.Repository;

public class FunkoRepository (Context dataBaseContext, ILogger<FunkoRepository> logger) : IFunkoRepository
{

    public async Task<Funko?> GetByIdAsync(long id)
    {
        logger.LogDebug("Consultando Funko por id en BD: {Id}", id);
        // Usar Include para cargar la relación Category de forma eager loading
        // Esto previene NullReferenceException cuando se accede a funko.Category.Nombre
        var foundFunko = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);
        
        return foundFunko;
    }
    public async Task<(IEnumerable<Funko> Items, int TotalCount)> GetAllAsync(FilterDTO filter)
    {
        logger.LogDebug("Consultando Funkos con filtros - Nombre: {Nombre}, Categoria: {Categoria}, MaxPrecio: {MaxPrecio}, Page: {Page}",
            filter.Nombre, filter.Categoria, filter.MaxPrecio, filter.Page);
        
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

        logger.LogDebug("Consulta de Funkos completada, encontrados: {Total}", totalCount);
        return (items, totalCount);
    }
    
    // OJO, devuelve IQueryable<Funko>, NO Task<List<Funko>>, porque se usa
    //el AsNoTracking() para ganar velocidad al hacer consultas con GraphQL
    public IQueryable<Funko> FindAllAsNoTracking()
    {
        //NO tenemos un .toList()
        return dataBaseContext.Funkos.AsNoTracking();
    }
    
    public async Task<Funko> CreateAsync(Funko funko)
    {
        logger.LogDebug("Guardando nuevo Funko en BD: {Nombre}", funko.Nombre);
        var savedFunko = await dataBaseContext.Funkos.AddAsync(funko);
        await dataBaseContext.SaveChangesAsync();
        logger.LogDebug("Funko guardado en BD con id: {Id}", savedFunko.Entity.Id);
        return savedFunko.Entity;
    }

    public async Task<Funko?> UpdateAsync(long id, Funko funko)
    {
        logger.LogDebug("Actualizando Funko en BD con id: {Id}", id);
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
            logger.LogDebug("Funko con id {Id} actualizado en BD", id);
            return foundFunko;
        }

        logger.LogDebug("Funko con id {Id} no encontrado en BD para actualizar", id);
        return null;
    }
    

    public async Task<Funko?> DeleteAsync(long id)
    {
        logger.LogDebug("Eliminando Funko de BD con id: {Id}", id);
        // Cambiamos FindAsync por FirstOrDefaultAsync con Include
        var foundFunko = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (foundFunko != null)
        {
            dataBaseContext.Funkos.Remove(foundFunko);
            await dataBaseContext.SaveChangesAsync();
            logger.LogDebug("Funko con id {Id} eliminado de BD", id);
            return foundFunko; // Ahora este objeto sí lleva su Category dentro
        }
    
        logger.LogDebug("Funko con id {Id} no encontrado en BD para eliminar", id);
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