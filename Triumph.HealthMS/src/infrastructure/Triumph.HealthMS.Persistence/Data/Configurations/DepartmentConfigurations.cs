namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class DepartmentConfigurations : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(d => new { d.TenantId, d.FacilityId })
            .IsUnique(false);
        
        builder.HasOne(d => d.HealthFacility)
            .WithMany(hf => hf.Departments)
            .HasForeignKey(d => d.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.TenantId, d.FacilityId });
    }
}