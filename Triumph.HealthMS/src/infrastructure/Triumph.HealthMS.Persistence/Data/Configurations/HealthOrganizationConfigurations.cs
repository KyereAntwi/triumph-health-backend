namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class HealthOrganizationConfigurations : IEntityTypeConfiguration<HealthOrganization>
{
    public void Configure(EntityTypeBuilder<HealthOrganization> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.OrganizationTitle)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.MainPhone)
            .IsRequired()
            .HasMaxLength(15);
        
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.HasMany(x => x.TenantSubscriptions)
            .WithOne(x => x.HealthOrganization)
            .HasForeignKey(x => x.HealthOrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.HealthFacilities)
            .WithOne(x => x.HealthOrganization)
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}