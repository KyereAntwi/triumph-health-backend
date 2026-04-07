namespace Triumph.HealthMS.Persistence.Services;

public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }
    public string UserId { get; set; } = default!;
    public bool IsAuthenticated { get; set; }
}