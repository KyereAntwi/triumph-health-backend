namespace Triumph.HealthMS.Persistence.DI;

public static class RegisterPersistenceLayer
{
    public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantContext, TenantContext>();

        services.AddScoped<AuditingInterceptor>();
        services.AddDbContext<IApplicationDbContext, AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("postgres"));
            options.AddInterceptors(sp.GetRequiredService<AuditingInterceptor>());
        });

        // for internal action logs
        services.AddMarten(options =>
        {
            options.Connection(configuration.GetConnectionString("postgres")!);
            options.DatabaseSchemaName = "audit";
            options.Schema.For<AuditLog>();
        })
        .UseLightweightSessions();
        
        services.AddScoped<IPermissionsServices, PermissionsServices>();
        
        return services;
    }
}