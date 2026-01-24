using FunkoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApi.DataBase;

public class Context(DbContextOptions options) : DbContext(options)
{
    public DbSet<Funko> Funkos { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        SeedData(modelBuilder); // Llamamos al metodo para poblar la BD
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // GUIDs estáticos para mantener consistencia
        var idPokemon = Guid.Parse("722f9661-8631-419b-8903-34e9e0339d01");
        var idMarvel = Guid.Parse("2974914c-1123-455b-8d00-4b693e5e463a");
        var idWow = Guid.Parse("3f4e3c98-1e96-487b-9494-28e44e233633");
        var idTerror = Guid.Parse("a5b6d5f7-6c2e-4b9e-9e4a-4d2d6f5c8e1a");

        var c1 = new Category { Id = idPokemon, Nombre = "POKEMON"};
        var c2 = new Category { Id = idMarvel, Nombre =  "MARVEL"};
        var c3 = new Category { Id = idWow, Nombre = "WOW" };
        var c4 = new Category { Id = idTerror, Nombre = "TERROR"};
        

        modelBuilder.Entity<Category>().HasData(c1, c2, c3, c4);

        modelBuilder.Entity<Funko>().HasData(
            new Funko { 
                Id = 1, 
                Nombre = "Pikachu", 
                Precio = 12.99, 
                CategoryId = c1.Id // POKEMON
            },
            new Funko { 
                Id = 2, 
                Nombre = "Charmander", 
                Precio = 12.99, 
                CategoryId = c1.Id // POKEMON
            },
            new Funko { 
                Id = 3, 
                Nombre = "Iron Man (Mark 85)", 
                Precio = 15.50, 
                CategoryId = c2.Id // MARVEL
            },
            new Funko { 
                Id = 4, 
                Nombre = "Spider-Man (No Way Home)", 
                Precio = 15.50, 
                CategoryId = c2.Id // MARVEL
            },
            new Funko { 
                Id = 5, 
                Nombre = "Arthas (The Lich King)", 
                Precio = 22.00, 
                CategoryId = c3.Id // WOW
            },
            new Funko { 
                Id = 6, 
                Nombre = "Sylvanas Windrunner", 
                Precio = 20.00, 
                CategoryId = c3.Id // WOW
            },
            new Funko { 
                Id = 7, 
                Nombre = "Illidan Stormrage", 
                Precio = 21.00, 
                CategoryId = c3.Id // WOW
            },
            new Funko { 
                Id = 8, 
                Nombre = "Freddy Krueger", 
                Precio = 14.50, 
                CategoryId = c4.Id // TERROR
            },
            new Funko { 
                Id = 9, 
                Nombre = "Michael Myers", 
                Precio = 14.50, 
                CategoryId = c4.Id // TERROR
            },
            new Funko { 
                Id = 10, 
                Nombre = "Mewtwo", 
                Precio = 18.00, 
                CategoryId = c1.Id // POKEMON
            }
        );
    }
}