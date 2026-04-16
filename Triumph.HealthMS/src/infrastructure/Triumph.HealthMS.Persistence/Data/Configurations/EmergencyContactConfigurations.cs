namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class EmergencyContactConfigurations : IEntityTypeConfiguration<EmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(e => e.Email)
            .HasMaxLength(225)
            .IsRequired(false);

        builder.Property(e => e.Relationship)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasOne(e => e.Patient)
            .WithMany(p => p.EmergencyContacts)
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(e => new {e.TenantId, e.PatientId, e.Deleted});
    }
}