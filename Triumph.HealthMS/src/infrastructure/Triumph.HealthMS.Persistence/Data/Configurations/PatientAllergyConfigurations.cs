namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientAllergyConfigurations : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.PatientAllergies)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Allergy)
            .WithMany()
            .HasForeignKey(x => x.AllergyId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(x => x.ReactionDetails)
            .HasMaxLength(500)
            .IsRequired();
        
        builder.HasIndex(x => new  {x.AllergyId, x.PatientId});
    }
}