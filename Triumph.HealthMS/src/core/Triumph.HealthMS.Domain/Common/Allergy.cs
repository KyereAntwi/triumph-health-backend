namespace Triumph.HealthMS.Domain.Common;

public class Allergy : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}