namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientConfigurations : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PassportNumber)
            .HasMaxLength(13)
            .IsRequired(false);
        
        builder.Property(x => x.NationalIdNumber)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(x => x.HomeAddress)
            .HasMaxLength(250);

        builder.Property(x => x.BloodGroup)
            .HasMaxLength(3)
            .IsRequired(false);
        
        builder.Property(x => x.Genotype)
            .HasMaxLength(1)
            .IsRequired(false);
        
        builder.HasOne(x => x.ApplicationUser)
            .WithMany(x => x.Patients)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(x => x.HealthFacility)
            .WithMany(x => x.Patients)
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.Appointments)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.EmergencyContacts)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.PatientAllergies)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.PatientChronicConditions)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.Medications)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PatientProcedures)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.PatientLabTests)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.Visits)
            .WithOne(x => x.Patient)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => new {x.TenantId, x.FacilityId, x.ApplicationUserId})
            .IsUnique(false);
    }
}