namespace Triumph.HealthMS.Domain.Common;

public class Procedure : AuditableEntity
{
    public string Code { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}