using FunkoApi.Models;
using FunkoApi.Repository;
using HotChocolate;
using HotChocolate.Types;

namespace FunkoApi.GraphQL.Queries;

public class FunkoQueries
{
    private readonly ILogger<FunkoQueries> _logger;

    public FunkoQueries(ILogger<FunkoQueries> logger)
    {
        _logger = logger;
    }
    
    //Funkos
    [UseProjection]
    public IQueryable<Funko> GetAllFunkos([Service] IFunkoRepository funkoRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando todos los Funkos");
        return funkoRepository.FindAllAsNoTracking();
    }

    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Funko> GetAllFunkosPaged([Service] IFunkoRepository funkoRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando Funkos con paginación");
        return funkoRepository.FindAllAsNoTracking();
    }
    
    [UseFirstOrDefault]
    public async Task<Funko?> GetFunkoById(long id, [Service] IFunkoRepository funkoRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando Funko por id: {Id}", id);
        return await funkoRepository.GetByIdAsync(id);
    }
    

    //Categorías
    [UseProjection]
    public IQueryable<Category> GetAllCategories([Service] ICategoryRepository categoriaRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando todas las categorías");
        return categoriaRepository.FindAllAsNoTracking();
    }
    
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 10)]
    public IQueryable<Category> GetAllCategoriesPaged([Service] ICategoryRepository categoriaRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando categorías con paginación");
        return categoriaRepository.FindAllAsNoTracking();
    }
    
    [UseFirstOrDefault]
    public async Task<Category?> GetCategoryById(string id, [Service] ICategoryRepository categoriaRepository)
    {
        _logger.LogDebug("GraphQL Query: Consultando categoría por id: {Id}", id);
        return await categoriaRepository.GetByIdAsync(new Guid(id));
    }
    
}