namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class HealthFacilityConfigurations : IEntityTypeConfiguration<HealthFacility>
{
    public void Configure(EntityTypeBuilder<HealthFacility> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(x => x.Location)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.HasIndex(x => x.TenantId)
            .IsUnique(false);
        
        builder.HasOne(x => x.HealthOrganization)
            .WithMany(x => x.HealthFacilities)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.Patients)
            .WithOne(x => x.HealthFacility)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(x => x.Departments)
            .WithOne(x => x.HealthFacility)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}