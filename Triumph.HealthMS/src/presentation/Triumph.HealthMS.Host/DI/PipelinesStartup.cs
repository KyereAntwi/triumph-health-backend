namespace Triumph.HealthMS.Host.DI;

public static class PipelinesStartup
{
    public static WebApplication AddPipelines(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi("/openapi/{documentName}.json");

            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Triumph HealthMS API")
                    .WithTheme(ScalarTheme.Solarized)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithOAuth2Authentication(oauth =>
                    {
                        oauth.ClientId = "health-ms";
                        oauth.Scopes = ["openid", "profile"];
                    })
                    .AddPreferredSecuritySchemes("OAuth2");
            });

            // perform database update for any new migrations
            //using var scope = app.Services.CreateScope();
            //var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            //db.Database.Migrate();
        }

        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseMiddleware<TenantResolverMiddleware>();
        app.UseMiddleware<UserResourceAccessMiddleware>();
        app.UseAuthorization();

        app.MapGraphQL()
            .WithOptions(new HotChocolate.AspNetCore.GraphQLServerOptions
            {
                Tool =
                {
                    Enable = app.Environment.IsDevelopment(),
                    Title = "Triumph HealthMS GraphQL",
                    DisableTelemetry = true
                }
            });

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var versionedGroup = app.MapGroup("")
            .WithApiVersionSet(versionSet);

        versionedGroup.MapCarter();

        return app;
    }
}