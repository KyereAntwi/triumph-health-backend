namespace Triumph.HealthMS.Application.UnitTests.Features.Employees;

public class UpdateEmployeeRole : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly TestAppDbContext _dbContext;
    private readonly UpdateRoleCommandHandler _sut;

    private readonly Role _currentRole;
    private readonly Role _newRole;
    private readonly Employee _employee;
    private readonly EmployeeRole _initialEmployeeRole;

    public UpdateEmployeeRole()
    {
        _dbContext = MockDbContextFactory.Create();
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANANGE_EMPLOYEES, granted: true);

        // Seed current role and a new target role
        _currentRole = TestDataSeeder.SeedRole(_dbContext, "Nurse", "Registered Nurse");
        _newRole = TestDataSeeder.SeedRole(_dbContext, "Doctor", "Medical Doctor");

        // Seed employee with the current role
        var (employee, _, employeeRole) = TestDataSeeder.SeedEmployeeWithRole(
            _dbContext, _currentRole.Id);
        _employee = employee;
        _initialEmployeeRole = employeeRole;

        _sut = new UpdateRoleCommandHandler(_dbContext, _permissionsServices);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private UpdateRoleCommand CreateValidCommand(
        Guid? employeeId = null,
        Guid? roleId = null,
        DateTime? resumedDate = null) =>
        new(
            EmployeeId: employeeId ?? _employee.Id,
            RoleId: roleId ?? _newRole.Id,
            ResumedDate: resumedDate ?? DateTime.UtcNow);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Updated");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsEndedRoleAtOnPreviousRole()
    {
        var before = DateTime.UtcNow;
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var previousRole = await _dbContext.EmployeeRoles
            .FirstAsync(r => r.Id == _initialEmployeeRole.Id);
        previousRole.EndedRoleAt.Should().BeOnOrAfter(before);
        previousRole.EndedRoleAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesNewEmployeeRole()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var roles = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id)
            .ToListAsync();
        roles.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ValidCommand_NewRoleHasCorrectRoleId()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var newRole = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id && r.Id != _initialEmployeeRole.Id)
            .SingleAsync();
        newRole.RoleId.Should().Be(_newRole.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_NewRoleHasCorrectResumedDate()
    {
        var resumedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = CreateValidCommand(resumedDate: resumedDate);

        await _sut.Handle(command, CancellationToken.None);

        var newRole = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id && r.Id != _initialEmployeeRole.Id)
            .SingleAsync();
        newRole.ResumedRoleAt.Should().Be(resumedDate);
    }

    [Fact]
    public async Task Handle_ValidCommand_NewRoleLinkedToCorrectEmployee()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var newRole = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id && r.Id != _initialEmployeeRole.Id)
            .SingleAsync();
        newRole.EmployeeId.Should().Be(_employee.Id);
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
        result.Errors.Should().Contain(e => e.Contains("Forbidden"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotCreateNewRole()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var roles = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id)
            .ToListAsync();
        roles.Should().HaveCount(1); // only the initial role
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotModifyExistingRole()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var existingRole = await _dbContext.EmployeeRoles
            .FirstAsync(r => r.Id == _initialEmployeeRole.Id);
        existingRole.EndedRoleAt.Should().Be(_initialEmployeeRole.EndedRoleAt);
    }

    [Fact]
    public async Task Handle_NoPermission_SkipsSubsequentLookups()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANANGE_EMPLOYEES, Arg.Any<CancellationToken>())
            .Returns(false);
        // Use non-existent IDs — should still return 403, not 404
        var command = CreateValidCommand(
            employeeId: Guid.NewGuid(),
            roleId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(403);
    }

    #endregion

    #region Employee Not Found

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsNotFoundResponse()
    {
        var command = CreateValidCommand(employeeId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Not found");
        result.Errors.Should().Contain(e => e.Contains("Employee"));
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_DoesNotCreateNewRole()
    {
        var originalCount = await _dbContext.EmployeeRoles.CountAsync();
        var command = CreateValidCommand(employeeId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.EmployeeRoles.CountAsync()).Should().Be(originalCount);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ErrorContainsEmployeeId()
    {
        var nonExistentId = Guid.NewGuid();
        var command = CreateValidCommand(employeeId: nonExistentId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Errors.Should().Contain(e => e.Contains(nonExistentId.ToString()));
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
        result.Message.Should().Be("Not found");
        result.Errors.Should().Contain(e => e.Contains("Role"));
    }

    [Fact]
    public async Task Handle_RoleNotFound_DoesNotCreateNewEmployeeRole()
    {
        var originalCount = await _dbContext.EmployeeRoles.CountAsync();
        var command = CreateValidCommand(roleId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.EmployeeRoles.CountAsync()).Should().Be(originalCount);
    }

    [Fact]
    public async Task Handle_RoleNotFound_DoesNotModifyExistingRole()
    {
        var command = CreateValidCommand(roleId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        var existingRole = await _dbContext.EmployeeRoles
            .FirstAsync(r => r.Id == _initialEmployeeRole.Id);
        existingRole.EndedRoleAt.Should().Be(_initialEmployeeRole.EndedRoleAt);
    }

    [Fact]
    public async Task Handle_RoleNotFound_ErrorContainsRoleId()
    {
        var nonExistentRoleId = Guid.NewGuid();
        var command = CreateValidCommand(roleId: nonExistentRoleId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Errors.Should().Contain(e => e.Contains(nonExistentRoleId.ToString()));
    }

    #endregion

    #region Validator

    [Fact]
    public void Validator_ValidCommand_PassesValidation()
    {
        var validator = new UpdateRoleCommandValidator();
        var command = CreateValidCommand();

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyEmployeeId_FailsValidation()
    {
        var validator = new UpdateRoleCommandValidator();
        var command = CreateValidCommand(employeeId: Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EmployeeId");
    }

    [Fact]
    public void Validator_EmptyRoleId_FailsValidation()
    {
        var validator = new UpdateRoleCommandValidator();
        var command = CreateValidCommand(roleId: Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RoleId");
    }

    [Fact]
    public void Validator_DefaultResumedDate_FailsValidation()
    {
        var validator = new UpdateRoleCommandValidator();
        var command = CreateValidCommand(resumedDate: DateTime.MinValue);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResumedDate");
    }

    [Fact]
    public void Validator_MultipleInvalidFields_ReturnsAllErrors()
    {
        var validator = new UpdateRoleCommandValidator();
        var command = new UpdateRoleCommand(Guid.Empty, Guid.Empty, DateTime.MinValue);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_AssignSameRoleAgain_StillCreatesNewEntry()
    {
        // Reassign the same role (e.g., employee leaves and re-assumes same role)
        var command = CreateValidCommand(roleId: _currentRole.Id);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var roles = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id)
            .ToListAsync();
        roles.Should().HaveCount(2);
        roles.Should().OnlyContain(r => r.RoleId == _currentRole.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PreviousRoleEndedRoleAtIsBeforeOrEqualToNewResumedDate()
    {
        var resumedDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = CreateValidCommand(resumedDate: resumedDate);

        await _sut.Handle(command, CancellationToken.None);

        var previousRole = await _dbContext.EmployeeRoles
            .FirstAsync(r => r.Id == _initialEmployeeRole.Id);
        previousRole.EndedRoleAt.Should().BeOnOrBefore(resumedDate);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsChanges()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        // Re-query to ensure data was saved
        var totalRoles = await _dbContext.EmployeeRoles
            .Where(r => r.EmployeeId == _employee.Id)
            .CountAsync();
        totalRoles.Should().Be(2);
    }

    [Fact]
    public async Task Handle_CancellationRequested_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var command = CreateValidCommand();

        var act = () => _sut.Handle(command, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}