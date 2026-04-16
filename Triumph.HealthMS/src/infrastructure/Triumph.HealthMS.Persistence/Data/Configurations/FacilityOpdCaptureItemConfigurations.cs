namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class FacilityOpdCaptureItemConfigurations : IEntityTypeConfiguration<FacilityOpdCaptureItem>
{
    public void Configure(EntityTypeBuilder<FacilityOpdCaptureItem> builder)
    {
        builder.HasOne(f => f.HealthFacility)
            .WithMany(h => h.FacilityOpdCaptureItems)
            .HasForeignKey(f => f.FacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.OpdCaptureItem)
            .WithMany()
            .HasForeignKey(f => f.OpdCaptureItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(f => f.Code)
            .HasMaxLength(15)
            .IsRequired(false);
        
        builder.Property(f => f.FacilityNotesOnItem)
            .HasMaxLength(500)
            .IsRequired(false);
        
        builder.HasIndex(f => new {f.TenantId, f.FacilityId, f.OpdCaptureItemId});
    }
}