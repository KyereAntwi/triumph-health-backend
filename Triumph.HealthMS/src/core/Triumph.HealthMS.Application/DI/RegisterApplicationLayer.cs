namespace Triumph.HealthMS.Application.DI;

public static class RegisterApplicationLayer
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddMediatR(req =>
        {
            req.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            req.AddOpenBehavior(typeof(PerformanceDecorator<,>));
            req.AddOpenBehavior(typeof(RetryDecorator<,>));
        });
        
        return services;
    }
}