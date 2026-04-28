namespace Triumph.HealthMS.Application.UnitTests.Mocks;

/// <summary>
/// Seeds common reference data used across multiple test classes.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Seeds default permissions (MANAGE_HEALTH_FACILITIES, VIEW_PATIENTS_PROFILE, UPDATE_TENANT_INFO).
    /// Returns the seeded permissions for further reference.
    /// </summary>
    public static Permission[] SeedDefaultPermissions(TestAppDbContext dbContext)
    {
        var permissions = new[]
        {
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.MANAGE_HEALTH_FACILITIES, Description = "Manage facilities" },
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.VIEW_PATIENTS_PROFILE, Description = "View patients" },
            new Permission { Id = Guid.NewGuid(), Value = PermissionValue.UPDATE_TENANT_INFO, Description = "Update tenant" }
        };

        dbContext.Permissions.AddRange(permissions);
        dbContext.SaveChanges();
        return permissions;
    }

    /// <summary>
    /// Seeds a default subscription and returns its ID.
    /// </summary>
    public static Guid SeedDefaultSubscription(TestAppDbContext dbContext)
    {
        var subscriptionId = Guid.NewGuid();
        dbContext.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            Title = "Premium Plan",
            Description = "Premium subscription",
            ActivePeriodInMonths = 12
        });
        dbContext.SaveChanges();
        return subscriptionId;
    }

    /// <summary>
    /// Creates a default ITenantContext mock with the standard test user ID and tenant ID.
    /// </summary>
    public static ITenantContext CreateMockTenantContext(
        string? userId = null,
        Guid? tenantId = null,
        Guid? facilityId = null)
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.UserId.Returns(userId ?? TestConstants.TestUserId);
        tenantContext.TenantId.Returns(tenantId ?? TestConstants.TestTenantId);
        tenantContext.FacilityId.Returns(facilityId);
        return tenantContext;
    }

    /// <summary>
    /// Seeds a Role and returns it.
    /// </summary>
    public static Role SeedRole(
        TestAppDbContext dbContext,
        string title = "Doctor",
        string description = "Medical Doctor",
        Guid? tenantId = null)
    {
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            TenantId = tenantId ?? TestConstants.TestTenantId
        };
        dbContext.Roles.Add(role);
        dbContext.SaveChanges();
        return role;
    }

    /// <summary>
    /// Seeds an Employee with an ApplicationUser and an initial EmployeeRole.
    /// Returns the employee, user, and the initial role assignment.
    /// </summary>
    public static (Employee Employee, ApplicationUser User, EmployeeRole EmployeeRole) SeedEmployeeWithRole(
        TestAppDbContext dbContext,
        Guid roleId,
        Guid? tenantId = null,
        DateTime? resumedRoleAt = null)
    {
        var tid = tenantId ?? TestConstants.TestTenantId;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Employee",
            Email = "test.employee@test.com",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateOnly(1990, 1, 1),
            UserId = "temporal-user",
            OtherNames = string.Empty
        };
        dbContext.ApplicationUsers.Add(user);

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            TenantId = tid,
            EmployedAt = DateTime.UtcNow
        };
        dbContext.Employees.Add(employee);

        var employeeRole = new EmployeeRole
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            RoleId = roleId,
            ResumedRoleAt = resumedRoleAt ?? DateTime.UtcNow.AddMonths(-6),
            CreatedAt = resumedRoleAt ?? DateTime.UtcNow.AddMonths(-6)
        };
        dbContext.EmployeeRoles.Add(employeeRole);

        dbContext.SaveChanges();
        return (employee, user, employeeRole);
    }

    /// <summary>
    /// Seeds a Patient (with an ApplicationUser) and returns the patient.
    /// </summary>
    public static Patient SeedPatient(
        TestAppDbContext dbContext,
        Guid? tenantId = null,
        Guid? facilityId = null)
    {
        var tid = tenantId ?? TestConstants.TestTenantId;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Patient",
            LastName = "User",
            Email = "patient@test.com",
            PhoneNumber = "+0000000000",
            DateOfBirth = new DateOnly(1985, 3, 20),
            UserId = "Unassigned",
            OtherNames = string.Empty
        };
        dbContext.ApplicationUsers.Add(user);

        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            TenantId = tid,
            FacilityId = facilityId ?? Guid.NewGuid(),
            NationalIdNumber = "NAT-001",
            HomeAddress = "1 Test St"
        };
        dbContext.Patients.Add(patient);
        dbContext.SaveChanges();
        return patient;
    }

    /// <summary>
    /// Configures an IPermissionsServices mock granting or denying a specific permission.
    /// </summary>
    public static IPermissionsServices CreateMockPermissionsService(
        PermissionValue permission,
        bool granted = true)
    {
        var service = Substitute.For<IPermissionsServices>();
        service.HasPermission(permission, Arg.Any<CancellationToken>())
            .Returns(granted);
        return service;
    }
}

