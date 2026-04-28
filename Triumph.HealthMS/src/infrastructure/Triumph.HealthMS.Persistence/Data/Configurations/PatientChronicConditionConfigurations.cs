namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class PatientChronicConditionConfigurations : IEntityTypeConfiguration<PatientChronicCondition>
{
    public void Configure(EntityTypeBuilder<PatientChronicCondition> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.PatientChronicConditions)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.Condition)
            .WithMany()
            .HasForeignKey(x => x.ConditionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PatientId, x.ConditionId });
    }
}