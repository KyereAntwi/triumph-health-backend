namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class VisitConfigurations : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Visits)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(x => x.VisitReasons)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.HasIndex(x => new {x.TenantId, x.PatientId});
    }
}