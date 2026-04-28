namespace Triumph.HealthMS.Shared.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; set; }
    Guid? FacilityId { get; set; }
    string UserId { get; set; }
    bool IsAuthenticated { get; set; }
}