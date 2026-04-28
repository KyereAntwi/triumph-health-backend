namespace Triumph.HealthMS.Domain.Common;

public class Permission : BaseEntity
{
    public PermissionValue Value { get; set; }
    public string Description { get; set; } = string.Empty;
}