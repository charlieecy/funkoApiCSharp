using FunkoApi.Models;
using FunkoApi.Repository;
using HotChocolate;
using HotChocolate.Types;

namespace FunkoApi.GraphQL.Queries;

public class FunkoQueries
{
    
    //Funkos
    [UseProjection]
    public IQueryable<Funko> GetAllFunkos([Service] IFunkoRepository funkoRepository) =>
        funkoRepository.FindAllAsNoTracking();

    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Funko> GetAllFunkosPaged([Service] IFunkoRepository funkoRepository) =>
        funkoRepository.FindAllAsNoTracking();
    
    [UseFirstOrDefault]
    public async Task<Funko?> GetFunkoById(long id, [Service] IFunkoRepository funkoRepository) =>
        await funkoRepository.GetByIdAsync(id);
    

    //Categorías
    [UseProjection]
    public IQueryable<Category> GetAllCategories([Service] ICategoryRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();
    
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Category> GetAllCategoriesPaged([Service] ICategoryRepository categoriaRepository) =>
        categoriaRepository.FindAllAsNoTracking();
    
    [UseFirstOrDefault]
    public async Task<Category?> GetCategoryById(string id, [Service] ICategoryRepository categoriaRepository) =>
        await categoriaRepository.GetByIdAsync(new Guid(id));
    
}