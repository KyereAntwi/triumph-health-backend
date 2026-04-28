namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class LabTestConfigurations : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}