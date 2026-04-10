namespace Triumph.HealthMS.Application.UnitTests.Mocks;

/// <summary>
/// Seeds common reference data used across multiple test classes.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Seeds default permissions (MANAGE_HEALTH_FACILITIES, VIEW_PATIENTS_PROFILE, UPDATE_TENANT_INFO).
    /// Returns the seeded permissions for further reference.
    /// </summary>
    public static Permission[] SeedDefaultPermissions(TestAppDbContext dbContext)
    {
        var permissions = new[]
        {
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.MANAGE_HEALTH_FACILITIES, Description = "Manage facilities" },
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.VIEW_PATIENTS_PROFILE, Description = "View patients" },
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.UPDATE_TENANT_INFO, Description = "Update tenant" }
        };

        dbContext.Permissions.AddRange(permissions);
        dbContext.SaveChanges();
        return permissions;
    }

    /// <summary>
    /// Seeds a default subscription and returns its ID.
    /// </summary>
    public static Guid SeedDefaultSubscription(TestAppDbContext dbContext)
    {
        var subscriptionId = Guid.NewGuid();
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            Title = "Premium Plan",
            Description = "Premium subscription",
            ActivePeriodInMonths = 12
        });
        dbContext.SaveChanges();
        return subscriptionId;
    }

    /// <summary>
    /// Creates a default ITenantContext mock with the standard test user ID and tenant ID.
    /// </summary>
    public static ITenantContext CreateMockTenantContext(
        string? userId = null,
        Guid? tenantId = null)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns(userId ?? TestConstants.TestUserId);
        tenantContext.TenantId.Returns(tenantId ?? TestConstants.TestTenantId);
        return tenantContext;
    }
}

