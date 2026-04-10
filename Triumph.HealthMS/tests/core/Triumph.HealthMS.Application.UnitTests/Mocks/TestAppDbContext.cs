namespace Triumph.HealthMS.Application.UnitTests.Mocks;

/// <summary>
/// In-memory DbContext implementing IApplicationDbContext for testing.
/// </summary>
public class TestAppDbContext : DbContext, IApplicationDbContext
{
    public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<HealthOrganizationEntity> HealthOrganizations => Set<HealthOrganizationEntity>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<HealthFacility> HealthFacilities => Set<HealthFacility>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeePermission> EmployeePermissions => Set<EmployeePermission>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure all entities with keys
        modelBuilder.Entity<HealthOrganizationEntity>().HasKey(e => e.Id);
        modelBuilder.Entity<ApplicationUser>().HasKey(e => e.Id);
        modelBuilder.Entity<Subscription>().HasKey(e => e.Id);
        modelBuilder.Entity<Permission>().HasKey(e => e.Id);
        modelBuilder.Entity<Employee>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasMany(e => e.Permissions).WithOne().HasForeignKey(ep => ep.EmployeeId);
        });
        modelBuilder.Entity<EmployeePermission>().HasKey(e => e.Id);
        modelBuilder.Entity<Patient>().HasKey(e => e.Id);
        modelBuilder.Entity<TenantSubscription>().HasKey(e => e.Id);
        modelBuilder.Entity<HealthFacility>().HasKey(e => e.Id);
        modelBuilder.Entity<Announcement>().HasKey(e => e.Id);
        modelBuilder.Entity<Department>().HasKey(e => e.Id);
        modelBuilder.Entity<EmployeeRole>().HasKey(e => e.Id);
        modelBuilder.Entity<Role>().HasKey(e => e.Id);

        // Make audit fields optional for all AuditableEntity-derived types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).Property(nameof(AuditableEntity.CreatedBy)).IsRequired(false);
            }
        }
    }
}