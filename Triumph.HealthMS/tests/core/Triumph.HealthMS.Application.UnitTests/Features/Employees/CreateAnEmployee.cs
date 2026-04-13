namespace Triumph.HealthMS.Application.UnitTests.Features.Employees;

public class CreateAnEmployee : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TestAppDbContext _dbContext;
    private readonly CreateAnEmployeeCommandHandler _sut;

    private readonly Guid _testRoleId;
    private readonly Guid _testFacilityId;
    private readonly Guid _testDepartmentId;

    public CreateAnEmployee()
    {
        _permissionsServices = Substitute.For<IPermissionsServices>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        var tenantContext = TestDataSeeder.CreateMockTenantContext();
        var logger = Substitute.For<ILogger<CreateAnEmployeeCommandHandler>>();
        _dbContext = MockDbContextFactory.Create();

        // Default: user has permission
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(true);

        // Seed a role
        _testRoleId = Guid.NewGuid();
        _dbContext.Roles.Add(new Role
        {
            Id = _testRoleId,
            Title = "Doctor",
            Description = "Medical Doctor",
            TenantId = TestConstants.TestTenantId
        });

        // Seed a facility
        _testFacilityId = Guid.NewGuid();
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = _testFacilityId,
            Name = "Main Hospital",
            Address = "1 Main St",
            Location = "City Center",
            Email = "main@hospital.com",
            MainPhone = "+1234567890",
            TenantId = TestConstants.TestTenantId
        });

        // Seed a department
        _testDepartmentId = Guid.NewGuid();
        _dbContext.Departments.Add(new Department
        {
            Id = _testDepartmentId,
            Title = "Cardiology",
            FacilityId = _testFacilityId,
            TenantId = TestConstants.TestTenantId
        });

        // Seed permissions
        TestDataSeeder.SeedDefaultPermissions(_dbContext);

        _dbContext.SaveChanges();

        _sut = new CreateAnEmployeeCommandHandler(
            _dbContext,
            _publishEndpoint,
            tenantContext,
            logger,
            _permissionsServices);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private CreateAnEmployeeCommand CreateValidCommand(
        Guid? facilityId = null,
        Guid? departmentId = null,
        Guid? roleId = null,
        IEnumerable<string>? permissions = null) =>
        new(
            FirstName: "Jane",
            LastName: "Smith",
            Email: "jane.smith@test.com",
            PhoneNumber: "+1234567890",
            OtherNames: null,
            DateOfBirth: new DateOnly(1990, 5, 15),
            FacilityId: facilityId,
            DepartmentId: departmentId,
            RoleId: roleId ?? _testRoleId,
            EmployedAt: DateTime.UtcNow,
            Permissions: permissions);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Created An Employee");
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesApplicationUser()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(command.FirstName);
        user.LastName.Should().Be(command.LastName);
        user.Email.Should().Be(command.Email);
        user.PhoneNumber.Should().Be(command.PhoneNumber);
        user.DateOfBirth.Should().Be(command.DateOfBirth);
        user.UserId.Should().Be("temporal-user");
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesEmployee()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees.FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Id.Should().Be(result.Data.Id);
        employee.TenantId.Should().Be(TestConstants.TestTenantId);
    }

    [Fact]
    public async Task Handle_ValidCommandWithFacility_AssociatesFacility()
    {
        var command = CreateValidCommand(facilityId: _testFacilityId);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.HealthFacility)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.HealthFacility.Should().NotBeNull();
        employee.HealthFacility!.Id.Should().Be(_testFacilityId);
    }

    [Fact]
    public async Task Handle_ValidCommandWithDepartment_AssociatesDepartment()
    {
        var command = CreateValidCommand(departmentId: _testDepartmentId);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Department.Should().NotBeNull();
        employee.Department!.Id.Should().Be(_testDepartmentId);
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutFacilityOrDepartment_LeavesThemNull()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.HealthFacility)
            .Include(e => e.Department)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.HealthFacility.Should().BeNull();
        employee.Department.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCommand_AssignsRole()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.Roles)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Roles.Should().HaveCount(1);
        employee.Roles.First().RoleId.Should().Be(_testRoleId);
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutEmployedAt_UsesUtcNow()
    {
        var command = CreateValidCommand() with { EmployedAt = null };
        var before = DateTime.UtcNow;

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees.FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.EmployedAt.Should().BeOnOrAfter(before);
        employee.EmployedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommandWithEmployedAt_UsesProvidedDate()
    {
        var employedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = CreateValidCommand() with { EmployedAt = employedAt };

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees.FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.EmployedAt.Should().Be(employedAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesEmployeeCreatedEvent()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<EmployeeCreatedEvent>(e =>
                e.CreatedBy == TestConstants.TestUserId &&
                e.ResourceType == nameof(Employee)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsExactlyOneEmployeeAndOneUser()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(1);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(1);
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbiddenResponse()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Forbidden");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotCreateEntities()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoPermission_SkipsValidation()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand() with { FirstName = "" }; // invalid but should return 403

        var result = await _sut.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(403);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyFirstName_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { FirstName = "" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_FirstNameTooShort_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { FirstName = "A" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_FirstNameTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { FirstName = new string('A', 51) };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyLastName_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { LastName = "" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_LastNameTooShort_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { LastName = "B" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { Email = "" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { Email = "not-an-email" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyPhoneNumber_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { PhoneNumber = "" };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_PhoneNumberTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { PhoneNumber = new string('1', 16) };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_OtherNamesTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { OtherNames = new string('A', 51) };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_FutureDateOfBirth_ReturnsValidationFailure()
    {
        var command = CreateValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)) };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyRoleId_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(roleId: Guid.Empty);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidPermissionString_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(permissions: ["NOT_A_REAL_PERMISSION"]);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        var command = CreateValidCommand() with
        {
            FirstName = "",
            LastName = "",
            Email = "bad",
            PhoneNumber = ""
        };

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersistEntities()
    {
        var command = CreateValidCommand() with { FirstName = "" };

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Facility Not Found

    [Fact]
    public async Task Handle_FacilityNotFound_ReturnsNotFoundResponse()
    {
        var command = CreateValidCommand(facilityId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Not Found");
        result.Errors.Should().Contain(e => e.Contains("Health Facility"));
    }

    [Fact]
    public async Task Handle_FacilityNotFound_DoesNotCreateEntities()
    {
        var command = CreateValidCommand(facilityId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Department Not Found

    [Fact]
    public async Task Handle_DepartmentNotFound_ReturnsNotFoundResponse()
    {
        var command = CreateValidCommand(departmentId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain(e => e.Contains("Department"));
    }

    [Fact]
    public async Task Handle_DepartmentNotFound_DoesNotCreateEntities()
    {
        var command = CreateValidCommand(departmentId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Role Not Found

    [Fact]
    public async Task Handle_RoleNotFound_ReturnsNotFoundResponse()
    {
        var command = CreateValidCommand(roleId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain(e => e.Contains("Role"));
    }

    [Fact]
    public async Task Handle_RoleNotFound_DoesNotCreateEntities()
    {
        var command = CreateValidCommand(roleId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Permissions Assignment

    [Fact]
    public async Task Handle_WithValidPermissions_AssignsPermissions()
    {
        var command = CreateValidCommand(permissions: [
            nameof(PermissionValue.MANAGE_HEALTH_FACILITIES),
            nameof(PermissionValue.VIEW_PATIENTS_PROFILE)
        ]);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Permissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoPermissions_CreatesEmployeeWithoutPermissions()
    {
        var command = CreateValidCommand(permissions: null);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptyPermissionsList_CreatesEmployeeWithoutPermissions()
    {
        var command = CreateValidCommand(permissions: []);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentPermission_ReturnsNotFoundResponse()
    {
        // Seed only 1 of the 2 requested; DELETE_PATIENTS not seeded
        var command = CreateValidCommand(permissions: [
            nameof(PermissionValue.MANAGE_HEALTH_FACILITIES),
            nameof(PermissionValue.DELETE_PATIENTS)
        ]);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain(e => e.Contains("permissions"));
    }

    [Fact]
    public async Task Handle_PermissionsNotFound_DoesNotCreateEntities()
    {
        var command = CreateValidCommand(permissions: [
            nameof(PermissionValue.MANAGE_HEALTH_FACILITIES),
            nameof(PermissionValue.DELETE_PATIENTS)
        ]);

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Event Publishing

    [Fact]
    public async Task Handle_PublishFails_StillReturnsSuccess()
    {
        _publishEndpoint.Publish(Arg.Any<EmployeeCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Message bus failure"));
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_PublishFails_EmployeeStillPersisted()
    {
        _publishEndpoint.Publish(Arg.Any<EmployeeCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Message bus failure"));
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Employees.CountAsync()).Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithOtherNames_PersistsOtherNames()
    {
        var command = CreateValidCommand() with { OtherNames = "Middle" };

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.OtherNames.Should().Be("Middle");
    }

    [Fact]
    public async Task Handle_WithoutOtherNames_PersistsEmptyString()
    {
        var command = CreateValidCommand(); // OtherNames = null

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.OtherNames.Should().Be(string.Empty);
    }

    [Fact]
    public async Task Handle_WithFacilityAndDepartment_AssociatesBoth()
    {
        var command = CreateValidCommand(facilityId: _testFacilityId, departmentId: _testDepartmentId);

        await _sut.Handle(command, CancellationToken.None);

        var employee = await _dbContext.Employees
            .Include(e => e.HealthFacility)
            .Include(e => e.Department)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.HealthFacility!.Id.Should().Be(_testFacilityId);
        employee.Department!.Id.Should().Be(_testDepartmentId);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChecksPermission()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        await _permissionsServices.Received(1)
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>());
    }

    #endregion
}

