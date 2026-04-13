namespace Triumph.HealthMS.Application.UnitTests.Features.Employees;

public class LinkEmployeeAccount : IDisposable
{
    private readonly TestAppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly LinkInvitedEmployeeToCreatedAccountCommandHandler _sut;

    private readonly Employee _employee;
    private readonly ApplicationUser _applicationUser;

    private const string InvitedUserId = "temporal-user";
    private const string CurrentUserId = TestConstants.TestUserId; // "user-123"

    public LinkEmployeeAccount()
    {
        _dbContext = MockDbContextFactory.Create();
        _tenantContext = TestDataSeeder.CreateMockTenantContext();

        // Seed a role, then an employee with the default "temporal-user" UserId
        var role = TestDataSeeder.SeedRole(_dbContext);
        var (employee, user, _) = TestDataSeeder.SeedEmployeeWithRole(_dbContext, role.Id);
        _employee = employee;
        _applicationUser = user;

        _sut = new LinkInvitedEmployeeToCreatedAccountCommandHandler(_dbContext, _tenantContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private LinkInvitedEmployeeToCreatedAccountCommand CreateValidCommand(Guid? employeeId = null) =>
        new(EmployeeId: employeeId ?? _employee.Id);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsEmployeeIdAsData()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Data.Should().Be(_employee.Id.ToString());
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesApplicationUserUserId()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsChange()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        // Re-query to confirm persistence
        var user = await _dbContext.ApplicationUsers
            .AsNoTracking()
            .FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_DoesNotModifyOtherUserFields()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.FirstName.Should().Be(_applicationUser.FirstName);
        user.LastName.Should().Be(_applicationUser.LastName);
        user.Email.Should().Be(_applicationUser.Email);
        user.PhoneNumber.Should().Be(_applicationUser.PhoneNumber);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyEmployeeId_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(employeeId: Guid.Empty);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
        result.Errors.Should().Contain(e => e.Contains("Employee Id"));
    }

    [Fact]
    public async Task Handle_EmptyEmployeeId_DoesNotModifyData()
    {
        var command = CreateValidCommand(employeeId: Guid.Empty);

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(InvitedUserId);
    }

    #endregion

    #region Validator Direct Tests

    [Fact]
    public void Validator_ValidEmployeeId_PassesValidation()
    {
        var validator = new LinkInvitedEmployeeToCreatedAccountCommandValidator();
        var command = CreateValidCommand();

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_EmptyEmployeeId_FailsValidation()
    {
        var validator = new LinkInvitedEmployeeToCreatedAccountCommandValidator();
        var command = CreateValidCommand(employeeId: Guid.Empty);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.PropertyName.Should().Be("EmployeeId");
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
        result.Message.Should().Be("Not Found");
        result.Errors.Should().Contain("Employee not found");
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_DoesNotModifyAnyUser()
    {
        var command = CreateValidCommand(employeeId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(InvitedUserId);
    }

    #endregion

    #region Already Linked

    [Fact]
    public async Task Handle_AlreadyLinkedToCurrentUser_ReturnsBadRequest()
    {
        // First link the employee
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        // Attempt to link again — UserId now matches tenantContext.UserId
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Already Linked");
        result.Errors.Should().Contain(e => e.Contains("already linked"));
    }

    [Fact]
    public async Task Handle_AlreadyLinkedToCurrentUser_DoesNotModifyUserId()
    {
        // First link
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        // Attempt again
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(CurrentUserId); // unchanged from the first link
    }

    [Fact]
    public async Task Handle_UserIdAlreadyMatchesAtSetup_ReturnsBadRequest()
    {
        // Arrange: set the user's UserId to the current user before calling handler
        _applicationUser.UserId = CurrentUserId;
        _dbContext.ApplicationUsers.Update(_applicationUser);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Already Linked");
    }

    #endregion

    #region Different User Contexts

    [Fact]
    public async Task Handle_DifferentUser_LinksToThatUser()
    {
        const string anotherUserId = "another-user-456";
        var tenantContext = TestDataSeeder.CreateMockTenantContext(userId: anotherUserId);
        var sut = new LinkInvitedEmployeeToCreatedAccountCommandHandler(_dbContext, tenantContext);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var user = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user.UserId.Should().Be(anotherUserId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_CancellationRequested_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var command = CreateValidCommand();

        var act = () => _sut.Handle(command, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Handle_MultipleEmployeesExist_LinksOnlyCorrectOne()
    {
        // Seed a second employee
        var role = TestDataSeeder.SeedRole(_dbContext, "Surgeon", "General Surgeon");
        var (_, secondUser, _) = TestDataSeeder.SeedEmployeeWithRole(_dbContext, role.Id);

        var command = CreateValidCommand(); // targets _employee

        await _sut.Handle(command, CancellationToken.None);

        // First employee linked
        var user1 = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == _applicationUser.Id);
        user1.UserId.Should().Be(CurrentUserId);

        // Second employee untouched
        var user2 = await _dbContext.ApplicationUsers.FirstAsync(u => u.Id == secondUser.Id);
        user2.UserId.Should().Be(InvitedUserId);
    }

    #endregion
}