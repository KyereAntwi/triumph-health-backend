namespace Triumph.HealthMS.Persistence.Data.Configurations;

public class ApplicationUserConfigurations : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => x.UserId);
        
        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .HasMaxLength(225);
        
        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(x => x.DateOfBirth)
            .IsRequired()
            .HasConversion(
                v => v.ToString("yyyy-MM-dd"),
                v => DateOnly.Parse(v));
        
        builder.Property(x => x.OtherNames)
            .HasMaxLength(50);
        
        builder.HasMany(x  => x.Patients)
            .WithOne(x => x.ApplicationUser)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}