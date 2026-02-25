using FunkoApi.GraphQL.Mutations;
using FunkoApi.GraphQL.Queries;
using FunkoApi.GraphQL.Subscriptions;
using FunkoApi.GraphQL.Types;

namespace FunkoApi.Infraestructure;

public static class GraphQLConfig
{

    public static IServiceCollection AddGraphQL(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<FunkoQueries>()
            .AddMutationType<FunkoMutations>()
            .AddSubscriptionType<FunkoSubscriptions>()
            .AddType<FunkoType>()
            .AddType<CategoryType>()
            .AddInMemorySubscriptions()
            .AddAuthorization()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            // Para obtener errores detallados
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

        return services;
    }
}
