namespace Triumph.HealthMS.Persistence.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        ApplyDeletedFilter(modelBuilder);
        ApplyTenantFilter(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var auditableEntries = ChangeTracker.Entries<AuditableEntity>();
        foreach (var entry in auditableEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Id = Guid.CreateVersion7();
                    entry.Entity.CreatedBy = _tenantContext.UserId;
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedBy = _tenantContext.UserId;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(TenantEntity).IsAssignableFrom(entityType.ClrType)) continue;
            
            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(this, [builder]);
        }
    }

    private void ApplyDeletedFilter(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType)) continue;
            
            var method = typeof(AppDbContext)
                .GetMethod(nameof(SetDeletedFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            
            method.Invoke(this, [builder]);
        }
    }
    
    private void SetTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : TenantEntity
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
    }

    private void SetDeletedFilter<TEntity>(ModelBuilder builder)
        where TEntity : AuditableEntity
    {
        builder.Entity<TEntity>()
            .HasQueryFilter(e => !e.Deleted);
    }

    public DbSet<OpdCaptureItem> OpdCaptureItems => Set<OpdCaptureItem>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<ChronicCondition> ChronicConditions => Set<ChronicCondition>();
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<HealthOrganization> HealthOrganizations => Set<HealthOrganization>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<HealthFacility>  HealthFacilities => Set<HealthFacility>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EmployeePermission> EmployeePermissions => Set<EmployeePermission>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientChronicCondition> PatientChronicConditions => Set<PatientChronicCondition>();
    public DbSet<PatientLabTest> PatientLabTests => Set<PatientLabTest>();
    public DbSet<PatientProcedure> PatientProcedures => Set<PatientProcedure>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<FacilityOpdCaptureItem> FacilityOpdCaptureItems => Set<FacilityOpdCaptureItem>();
    public DbSet<PatientFacilityOpdCaptureItem> PatientFacilityOpdCaptureItems => Set<PatientFacilityOpdCaptureItem>();
}