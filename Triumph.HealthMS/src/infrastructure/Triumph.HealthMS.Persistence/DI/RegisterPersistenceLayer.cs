namespace Triumph.HealthMS.Persistence.DI;

public static class RegisterPersistenceLayer
{
    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        
        return services;
    }
}