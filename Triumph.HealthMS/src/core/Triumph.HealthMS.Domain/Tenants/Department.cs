namespace Triumph.HealthMS.Domain.Tenants;

public class Department : TenantEntity
{
    public string Title { get; set; } = string.Empty;

    public Guid FacilityId { get; set; }
    public HealthFacility? HealthFacility { get; set; }
}