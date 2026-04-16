namespace Triumph.HealthMS.Domain.Common;

public class Drug : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
}