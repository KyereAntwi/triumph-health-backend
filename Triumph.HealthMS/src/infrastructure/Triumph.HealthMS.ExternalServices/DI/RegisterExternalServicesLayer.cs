namespace Triumph.HealthMS.ExternalServices.DI;

public static class RegisterExternalServicesLayer
{
    public static IServiceCollection AddExternalServicesLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var authorityBase = configuration["AuthServer:Authority"]?.TrimEnd('/');
        var realm = configuration["AuthServer:Realm"]?.TrimStart('/');
        var authority = $"{authorityBase}/{realm}";

        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.Audience = configuration["AuthServer:Audience"];
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine("[AUTH] Token received: " + (string.IsNullOrEmpty(context.Token) ? "NO TOKEN" : "token present (" + context.Token.Length + " chars)"));
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("[AUTH] Authentication failed: " + context.Exception.GetType().Name + ": " + context.Exception.Message);
                        if (context.Exception.InnerException != null)
                            Console.WriteLine("[AUTH] Inner: " + context.Exception.InnerException.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var issuer = context.Principal?.FindFirst("iss")?.Value;
                        Console.WriteLine("[AUTH] Token validated! Issuer: " + issuer);
                        Console.WriteLine("[AUTH] User: " + context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine("[AUTH] Challenge issued. Error: " + context.Error + ", Description: " + context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });
        
        services.AddAuthorization();
        
        // mass transit
        services.AddMassTransit(config =>
        {
            config.SetKebabCaseEndpointNameFormatter();
            
            // register consumers globally
            config.AddConsumers(typeof(RegisterExternalServicesLayer).Assembly);
            
            //config.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            
            // for rabbitmq
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(configuration["RabbitMQ:Host"] ?? throw new InvalidOperationException()), host =>
                {
                    host.Username(configuration["RabbitMQ:Username"] ?? throw new InvalidOperationException());
                    host.Password(configuration["RabbitMQ:Password"] ?? throw new InvalidOperationException());
                });
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}