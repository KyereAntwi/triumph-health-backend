namespace Triumph.HealthMS.Commands.DI;

public static class RegisterCommandServices
{
    public static IServiceCollection AddCommandServices(this IServiceCollection services)
    {
        AddSwagger(services);
        services.AddHttpContextAccessor();
        services.AddEndpointsApiExplorer();
        
        services.AddCarter(null, configurator =>
        {
            var modules = typeof(RegisterCommandServices).Assembly
                .GetTypes()
                .Where(t => typeof(ICarterModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

            var withModuleMethod = typeof(CarterConfigurator).GetMethod(nameof(CarterConfigurator.WithModule));

            foreach (var module in modules)
            {
                withModuleMethod!.MakeGenericMethod(module).Invoke(configurator, null);
            }
        });
        
        return services;
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(static c =>
        {
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            //c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            //{
            //    {
            //        new OpenApiSecuritySchemeReference
            //        {
            //            Type = ReferenceType.SecurityScheme,
            //            Id = "Bearer"
            //        },
            //        new List<string>()
            //    }
            //});

            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Triumph.HealthMS API",

            });

            // c.OperationFilter<FileResultContentTypeOperationFilter>();
        });
    }
}