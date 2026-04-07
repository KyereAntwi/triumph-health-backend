namespace Triumph.HealthMS.Queries.DI;

public static class RegisterQueryServices
{
    public static IServiceCollection AddQueryServices(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<QueryBase>();
            
        return services;
    }
}