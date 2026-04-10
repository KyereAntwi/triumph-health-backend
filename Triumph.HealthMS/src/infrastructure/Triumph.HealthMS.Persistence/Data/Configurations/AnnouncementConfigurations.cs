namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class AnnouncementConfigurations : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Details)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.EntityId)
            .IsRequired();
        
        builder.HasIndex(e => e.EntityId);
    }
}