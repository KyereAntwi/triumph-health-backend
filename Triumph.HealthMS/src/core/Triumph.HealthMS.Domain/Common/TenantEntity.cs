namespace Triumph.HealthMS.Domain.Common;

public class TenantEntity : AuditableEntity
{
    public Guid TenantId { get; set; }
}