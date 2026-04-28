namespace Triumph.HealthMS.Domain.Tenants;

public class TenantSubscription : AuditableEntity
{
    public Guid HealthOrganizationId { get; set; }
    public HealthOrganization? HealthOrganization { get; set; }
    
    public Guid SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public bool IsActive { get; set; } = true;
}