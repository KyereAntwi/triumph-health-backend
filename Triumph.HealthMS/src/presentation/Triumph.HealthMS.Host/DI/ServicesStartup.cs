namespace Triumph.HealthMS.Host.DI;

public static class ServicesStartup
{
    public static WebApplication AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        
        // register layers
        builder.Services.AddApplicationLayer();
        builder.Services.AddPersistenceLayer(builder.Configuration);
        builder.Services.AddExternalServicesLayer(builder.Configuration);
        builder.Services.AddCommandServices();
        builder.Services.AddQueryServices();

        // aspire configurations
        builder.AddServiceDefaults();
        builder.AddRedisClient(connectionName: "redis");
        
        return builder.Build();
    }
}