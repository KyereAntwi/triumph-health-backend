namespace Triumph.HealthMS.Domain.Tenants.Employees;

public class EmployeePermission : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}