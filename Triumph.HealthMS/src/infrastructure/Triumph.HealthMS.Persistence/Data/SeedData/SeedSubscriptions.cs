namespace Triumph.HealthMS.Persistence.Data.SeedData;

public class SeedSubscriptions : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasData(
            new Subscription
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Basic Plan",
                ActivePeriodInMonths = 6,
                Description = "Access to basic features for 1 month."
            },
            
            new Subscription
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Standard Plan",
                ActivePeriodInMonths = 12,
                Description = "Access to standard features for 6 months."
            },
            
            new Subscription()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Free Plan",
                ActivePeriodInMonths = 1,
            }
            );
    }
}