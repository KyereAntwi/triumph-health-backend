namespace Triumph.HealthMS.Domain.Tenants.Employees;

public class Role : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}