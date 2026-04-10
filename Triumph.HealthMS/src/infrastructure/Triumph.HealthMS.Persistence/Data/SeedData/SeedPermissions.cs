namespace Triumph.HealthMS.Persistence.Data.SeedData;

public class SeedPermissions : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasData(
            new Permission
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Value = PermissionValue.VIEW_PATIENTS_PROFILE,
                Description = "View all patients biography belonging to a particular facility"
            },
            
            new Permission
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Value = PermissionValue.MANAGE_PATIENTS_PROFILE,
                Description = "Create and Update all patients biography belonging to a particular facility"
            },
            
            new Permission
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Value = PermissionValue.DELETE_PATIENTS,
                Description = "Delete a patient's records belonging to a particular facility"
            },
            
            new Permission
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Value = PermissionValue.MANAGE_HEALTH_FACILITIES,
                Description = "Create, Update and Delete health facilities for a particular tenant"
            }
        );
    }
}