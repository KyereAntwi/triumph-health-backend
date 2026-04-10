namespace Triumph.HealthMS.Host.DI;

public static class PipelinesStartup
{
    public static WebApplication AddPipelines(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Triumph.HealthMS API V1");
                options.RoutePrefix = string.Empty;
            });
            
            // perform database update for any new migrations
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        app.UseRouting();
        
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        
        app.UseAuthentication();
        app.UseMiddleware<TenantResolverMiddleware>();
        app.UseMiddleware<UserResourceAccessMiddleware>();
        app.UseAuthorization();
        
        app.MapGraphQL();
        app.MapDefaultEndpoints();
        app.MapCarter();
        
        return app;
    }
}