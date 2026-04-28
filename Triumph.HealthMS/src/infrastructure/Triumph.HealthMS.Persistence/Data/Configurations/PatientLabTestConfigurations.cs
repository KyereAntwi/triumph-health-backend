namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientLabTestConfigurations : IEntityTypeConfiguration<PatientLabTest>
{
    public void Configure(EntityTypeBuilder<PatientLabTest> builder)
    {
            builder.HasOne(x => x.Patient)
                .WithMany(x => x.PatientLabTests)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(x => x.FacilityLabTest)
                .WithMany()
                .HasForeignKey(x => x.FacilityLabTestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.PatientId, x.FacilityLabTestId });
    }
}