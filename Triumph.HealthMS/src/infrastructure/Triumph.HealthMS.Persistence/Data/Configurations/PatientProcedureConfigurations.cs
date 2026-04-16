namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientProcedureConfigurations : IEntityTypeConfiguration<PatientProcedure>
{
    public void Configure(EntityTypeBuilder<PatientProcedure> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.PatientProcedures)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.FacilityProcedure)
            .WithMany()
            .HasForeignKey(x => x.FacilityProcedureId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(x => x.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);
        
        builder.HasIndex(x => new { x.PatientId, x.FacilityProcedureId });
    }
}