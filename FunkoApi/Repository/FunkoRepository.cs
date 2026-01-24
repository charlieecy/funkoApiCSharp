using FunkoApi.DataBase;
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

    public async Task<List<Funko>> GetAllAsync()
    {
        // Include carga la relación Category para todos los Funkos
        // Evita el NullReferenceException en el mapper
        var funkos = await dataBaseContext.Funkos
            .Include(f => f.Category)
            .ToListAsync();
        
        return funkos;
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
}