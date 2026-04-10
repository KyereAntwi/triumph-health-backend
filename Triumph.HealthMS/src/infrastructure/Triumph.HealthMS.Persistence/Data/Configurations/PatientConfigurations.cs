namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientConfigurations : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(x => x.Id);
        
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
        
        builder.HasIndex(x => new {x.TenantId, x.FacilityId, x.ApplicationUserId})
            .IsUnique(false);
    }
}