namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class DrugConfigurations : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Description).HasMaxLength(1000);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(15);
        builder.Property(d => d.Prescription).HasMaxLength(1000);
    }
}