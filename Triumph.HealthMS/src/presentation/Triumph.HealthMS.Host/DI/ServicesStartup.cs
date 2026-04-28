namespace Triumph.HealthMS.Host.DI;

public static class ServicesStartup
{
    public static WebApplication AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                var config = context.ApplicationServices.GetRequiredService<IConfiguration>();
                var authorityBase = config["AuthServer:Authority"]?.TrimEnd('/');
                var realm = config["AuthServer:Realm"]?.TrimStart('/');
                var authority = $"{authorityBase}/{realm}";

                document.Info.Title = "Triumph HealthMS API";
                document.Info.Version = "v1";

                // Add OAuth2 security scheme to components
                if (document.Components is null)
                    document.Components = new OpenApiComponents();

                if (document.Components.SecuritySchemes is null)
                    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["OAuth2"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                            TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "OpenID" },
                                { "profile", "Profile" }
                            }
                        }
                    }
                };

                // Add global security requirement
                var schemeRef = new OpenApiSecuritySchemeReference("OAuth2", document);
                if (document.Security is null)
                    document.Security = [];

                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [schemeRef] = ["openid", "profile"]
                });

                return Task.CompletedTask;
            });
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (environment == "Development")
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day);
            });
        }
        else
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                    .WriteTo.Sentry(options =>
                    {
                        options.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                        options.MinimumEventLevel = LogEventLevel.Warning;
                        options.Dsn = context.Configuration["Sentry:Dsn"];
                        options.TracesSampleRate = 1.0;
                        options.EnableLogs = true;
                    });
            });
        }

        // register layers
        builder.Services.AddApplicationLayer();
        builder.Services.AddPersistenceLayer(builder.Configuration);
        builder.Services.AddExternalServicesLayer(builder.Configuration);
        builder.Services.AddCommandServices();
        builder.Services.AddQueryServices();

        // builder.Services.AddOpenTelemetry()
        //     .ConfigureResource(resource => resource.AddService("Triumph.HealthMS.Host"))
        //     .WithTracing(tracing =>
        //     {
        //         tracing
        //             .AddNpgsql();
        //     });
        //
        // builder.Logging.AddOpenTelemetry(logging =>
        // {
        //     logging.IncludeFormattedMessage = true;
        //     logging.IncludeScopes = true;a
        //     logging.AddOtlpExporter();
        // });

        builder.Services.AddProblemDetails();

        return builder.Build();
    }
}