namespace Triumph.HealthMS.Application.Interfaces;

public interface IApplicationDbContext
{
    public DbSet<Patient> Patients { get; }
    public DbSet<ApplicationUser> ApplicationUsers { get; }
    public DbSet<Permission> Permissions { get; }
    public DbSet<Subscription> Subscriptions { get; }
    public DbSet<HealthOrganization> HealthOrganizations { get; }
    public DbSet<TenantSubscription> TenantSubscriptions { get; }
    public DbSet<HealthFacility> HealthFacilities { get; }
    public DbSet<Announcement> Announcements { get; }
    public DbSet<Department> Departments { get; }
    public DbSet<EmployeePermission> EmployeePermissions { get; }
    public DbSet<EmployeeRole> EmployeeRoles { get; }
    public DbSet<Role> Roles { get; }
    public DbSet<Employee> Employees { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}