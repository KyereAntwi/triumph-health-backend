namespace Triumph.HealthMS.Domain.Tenants.Employees;

public class Employee : TenantEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    
    public virtual HealthFacility? HealthFacility { get; set; }
    public virtual Department? Department { get; set; }
    public ICollection<EmployeePermission> Permissions { get; set; } = [];
    public ICollection<EmployeeRole> Roles { get; set; } = [];

    [ForeignKey("TenantId")]
    public HealthOrganization? HealthOrganization { get; set; }

    public DateTime EmployedAt { get; set; }
}