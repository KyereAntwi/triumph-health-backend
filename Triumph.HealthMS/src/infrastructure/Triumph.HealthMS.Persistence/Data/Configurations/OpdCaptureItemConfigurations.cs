namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class OpdCaptureItemConfigurations : IEntityTypeConfiguration<OpdCaptureItem>
{
    public void Configure(EntityTypeBuilder<OpdCaptureItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}