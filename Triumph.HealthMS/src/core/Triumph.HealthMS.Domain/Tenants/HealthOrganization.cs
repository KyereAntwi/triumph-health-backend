namespace Triumph.HealthMS.Domain.Tenants;

public class HealthOrganization : AuditableEntity
{
    public string OrganizationTitle { get; set; } =  string.Empty;
    public string Address { get; set; } =  string.Empty;
    public string MainPhone { get; set; } =  string.Empty;
    public string Email { get; set; } =  string.Empty;
    public string? LogoUrl { get; set; }

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = [];
    public ICollection<HealthFacility> HealthFacilities { get; set; } = [];
    public ICollection<Announcement> Announcements { get; set; } = [];
}