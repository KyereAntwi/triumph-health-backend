namespace Triumph.HealthMS.Commands.DI;

public static class RegisterCommandServices
{
    public static IServiceCollection AddCommandServices(this IServiceCollection services)
    {
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

        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
        });
        
        return services;
    }
}