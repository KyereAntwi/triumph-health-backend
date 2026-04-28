namespace Triumph.HealthMS.Domain.Common;

public class ChronicCondition : AuditableEntity
{
    public string Condition { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}