namespace Triumph.HealthMS.Domain.Tenants;

public class Announcement : AuditableEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime ValidUntil { get; set; }
}