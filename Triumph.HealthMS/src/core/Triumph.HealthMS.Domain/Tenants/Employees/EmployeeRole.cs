namespace Triumph.HealthMS.Domain.Tenants.Employees;

public class EmployeeRole: AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime ResumedRoleAt { get; set; }
    public DateTime EndedRoleAt { get; set; }
}