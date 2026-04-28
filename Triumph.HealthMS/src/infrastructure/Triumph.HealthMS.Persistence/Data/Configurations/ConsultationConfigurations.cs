namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class ConsultationConfigurations : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.PatientId });
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Consultations)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.Consultations)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.Property(x => x.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);
    }
}