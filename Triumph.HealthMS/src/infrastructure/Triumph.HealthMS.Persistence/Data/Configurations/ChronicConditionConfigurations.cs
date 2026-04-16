namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class ChronicConditionConfigurations : IEntityTypeConfiguration<ChronicCondition>
{
    public void Configure(EntityTypeBuilder<ChronicCondition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Condition).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Details).HasMaxLength(500);
    }
}