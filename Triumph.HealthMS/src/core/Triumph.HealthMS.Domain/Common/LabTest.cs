namespace Triumph.HealthMS.Domain.Common;

public class LabTest : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}