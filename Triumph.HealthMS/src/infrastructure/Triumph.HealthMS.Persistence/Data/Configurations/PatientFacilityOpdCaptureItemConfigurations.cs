namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientFacilityOpdCaptureItemConfigurations : IEntityTypeConfiguration<PatientFacilityOpdCaptureItem>
{
    public void Configure(EntityTypeBuilder<PatientFacilityOpdCaptureItem> builder)
    {
        builder.HasOne(p => p.Patient)
            .WithMany()
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.FacilityOpdCaptureItem)
            .WithMany()
            .HasForeignKey(p => p.FacilityOpdCaptureItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(p => p.ValueCaptured)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(p => p.Notes)
            .HasMaxLength(500)
            .IsRequired(false);
    }
}