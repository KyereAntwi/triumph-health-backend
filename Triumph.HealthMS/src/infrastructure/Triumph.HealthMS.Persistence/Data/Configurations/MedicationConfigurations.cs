namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class MedicationConfigurations : IEntityTypeConfiguration<Medication>
{
    public void Configure(EntityTypeBuilder<Medication> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.HasOne(m => m.Patient)
            .WithMany(p => p.Medications)
            .HasForeignKey(m => m.PatientId);
        
        builder.HasOne(m => m.Drug);
        
        builder.Property(m => m.AdditionalPrescription)
            .HasMaxLength(1000)
            .IsRequired(false);
        
        builder.HasIndex(m => new {m.DrugId, m.PatientId});
    }
}