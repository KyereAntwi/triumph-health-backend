using Microsoft.EntityFrameworkCore.Design;

namespace Triumph.HealthMS.Persistence.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__postgres")
            ?? "Host=localhost;Database=triumph_healthms;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options, new TenantContext
        {
            TenantId = Guid.Empty,
            FacilityId = null,
            UserId = "design-time",
            IsAuthenticated = false
        });
    }
}
