namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class EmployeeConfigurations : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.ApplicationUser);
        builder.HasOne(e => e.HealthFacility)
            .WithMany();
        builder.HasOne(e => e.Department)
            .WithMany();
        builder.HasMany(e => e.Permissions)
            .WithOne(e => e.Employee)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Roles)
            .WithOne(e => e.Employee)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(e => e.TenantId);
    }
}