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
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Triumph.HealthMS.Host"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddNpgsql();
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter();
        });
        
        return builder.Build();
    }
}