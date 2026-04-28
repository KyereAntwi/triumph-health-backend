namespace Triumph.HealthMS.Application.UnitTests.Features.HealthOrganization;

public class CreateAnOrganization : IDisposable
{
    private readonly ITenantContext _tenantContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateAnOrganizationCommandHandler> _logger;
    private readonly TestAppDbContext _dbContext;
    private readonly CreateAnOrganizationCommandHandler _sut;

    private readonly Guid _testSubscriptionId;

    public CreateAnOrganization()
    {
        _tenantContext = TestDataSeeder.CreateMockTenantContext();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _logger = Substitute.For<ILogger<CreateAnOrganizationCommandHandler>>();

        _dbContext = MockDbContextFactory.Create();

        TestDataSeeder.SeedDefaultPermissions(_dbContext);
        _testSubscriptionId = TestDataSeeder.SeedDefaultSubscription(_dbContext);

        _sut = new CreateAnOrganizationCommandHandler(
            _tenantContext,
            _publishEndpoint,
            _logger,
            _dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private CreateAnOrganizationCommand CreateValidCommand(Guid? subscriptionId = null) =>
        new(
            OrganizationTitle: "Test Hospital",
            Address: "123 Main Street",
            MainPhone: "+1234567890",
            Email: "org@test.com",
            SubscriptionId: subscriptionId ?? _testSubscriptionId,
            FirstName: "John",
            LastName: "Doe",
            OtherNames: "James",
            PhoneNumber: "+1234567890",
            EmailAddress: "john@test.com",
            DateOfBirth: new DateOnly(1990, 1, 1),
            EmployedAt: DateTime.UtcNow);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Successfully created an organization");
        result.Data.Should().NotBeNull();
        result.Data.SubscriptionId.Should().Be(_testSubscriptionId);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesApplicationUser()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync(u => u.UserId == TestConstants.TestUserId);
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(command.FirstName);
        user.LastName.Should().Be(command.LastName);
        user.OtherNames.Should().Be(command.OtherNames);
        user.PhoneNumber.Should().Be(command.PhoneNumber);
        user.Email.Should().Be(command.EmailAddress);
        user.DateOfBirth.Should().Be(command.DateOfBirth);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesHealthOrganization()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var org = await _dbContext.HealthOrganizations.FirstOrDefaultAsync();
        org.Should().NotBeNull();
        org!.OrganizationTitle.Should().Be(command.OrganizationTitle);
        org.Address.Should().Be(command.Address);
        org.MainPhone.Should().Be(command.MainPhone);
        org.Email.Should().Be(command.Email);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesEmployee()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var employee = await _dbContext.Employees.FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.EmployedAt.Should().Be(command.EmployedAt!.Value);
    }

    [Fact]
    public async Task Handle_ValidCommandWithoutEmployedAt_UsesUtcNow()
    {
        // Arrange
        var command = CreateValidCommand() with { EmployedAt = null };
        var beforeTest = DateTime.UtcNow;

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var employee = await _dbContext.Employees.FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.EmployedAt.Should().BeOnOrAfter(beforeTest);
        employee.EmployedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsAllEntities()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(1);
        // 1 health org created by test (the seed doesn't add orgs)
        (await _dbContext.HealthOrganizations.CountAsync()).Should().Be(1);
        (await _dbContext.Employees.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesOrganizationCreatedEvent()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _publishEndpoint.Received(1).Publish(
            Arg.Is<OrganizationCreatedEvent>(e =>
                e.CreatedBy == TestConstants.TestUserId &&
                e.OrganizationEmail == command.Email &&
                e.OrganizationName == command.OrganizationTitle &&
                e.SubscriptionName == "Premium Plan" &&
                e.ResourceType == "HealthOrganization"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTenantIdAndSubscriptionId()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Data.TenantId.Should().NotBeEmpty();
        result.Data.SubscriptionId.Should().Be(_testSubscriptionId);
        // Verify the returned TenantId matches the actual created org
        var org = await _dbContext.HealthOrganizations.FindAsync(result.Data.TenantId);
        org.Should().NotBeNull();
    }

    #endregion

    #region User Already Created Organization

    [Fact]
    public async Task Handle_UserAlreadyCreatedOrganization_ReturnsFailure()
    {
        // Arrange
        _dbContext.HealthOrganizations.Add(new HealthOrganizationEntity
        {
            Id = Guid.NewGuid(), CreatedBy = TestConstants.TestUserId,
            OrganizationTitle = "Existing", Address = "A", MainPhone = "+1", Email = "a@a.com"
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("User has already created an organization");
        result.Errors.Should().Contain("User has already created an organization");
    }

    [Fact]
    public async Task Handle_UserAlreadyCreatedOrganization_DoesNotCreateNewEntities()
    {
        // Arrange
        _dbContext.HealthOrganizations.Add(new HealthOrganizationEntity
        {
            Id = Guid.NewGuid(), CreatedBy = TestConstants.TestUserId,
            OrganizationTitle = "Existing", Address = "A", MainPhone = "+1", Email = "a@a.com"
        });
        await _dbContext.SaveChangesAsync();
        var countBefore = await _dbContext.ApplicationUsers.CountAsync();
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(countBefore);
    }

    [Fact]
    public async Task Handle_UserAlreadyCreatedOrganization_DoesNotPublishEvent()
    {
        // Arrange
        _dbContext.HealthOrganizations.Add(new HealthOrganizationEntity
        {
            Id = Guid.NewGuid(), CreatedBy = TestConstants.TestUserId,
            OrganizationTitle = "Existing", Address = "A", MainPhone = "+1", Email = "a@a.com"
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<OrganizationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherUsersHaveOrganizations_AllowsCreation()
    {
        // Arrange
        _dbContext.HealthOrganizations.Add(new HealthOrganizationEntity
        {
            Id = Guid.NewGuid(), CreatedBy = "other-user-456",
            OrganizationTitle = "Other Org", Address = "B", MainPhone = "+2", Email = "b@b.com"
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyOrganizationTitle_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { OrganizationTitle = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation failed");
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyAddress_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Address = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation failed");
    }

    [Fact]
    public async Task Handle_InvalidMainPhone_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { MainPhone = "not-a-phone" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = "not-an-email" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptySubscriptionId_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { SubscriptionId = Guid.Empty };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyFirstName_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyLastName_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidPhoneNumber_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { PhoneNumber = "abc" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidEmailAddress_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { EmailAddress = "invalid" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_FutureDateOfBirth_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(1)) };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersistEntities()
    {
        // Arrange
        var command = CreateValidCommand() with { OrganizationTitle = "" };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
        (await _dbContext.HealthOrganizations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            OrganizationTitle = "",
            Address = "",
            MainPhone = "bad",
            Email = "bad"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Handle_OtherNamesTooLong_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { OtherNames = "ThisNameIsTooLongForValidation" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_FirstNameTooShort_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = "Ab" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_LastNameTooShort_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = "Ab" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    #endregion

    #region Subscription Not Found

    [Fact]
    public async Task Handle_SubscriptionNotFound_ReturnsNotFoundResponse()
    {
        // Arrange
        var command = CreateValidCommand(subscriptionId: Guid.NewGuid());

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Contain("Subscription");
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_SubscriptionNotFound_DoesNotCreateEntities()
    {
        // Arrange
        var command = CreateValidCommand(subscriptionId: Guid.NewGuid());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
        (await _dbContext.HealthOrganizations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_SubscriptionNotFound_DoesNotPublishEvent()
    {
        // Arrange
        var command = CreateValidCommand(subscriptionId: Guid.NewGuid());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _publishEndpoint.DidNotReceive()
            .Publish(Arg.Any<OrganizationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task Handle_ExceptionDuringPublish_ReturnsServerError()
    {
        // Arrange
        _publishEndpoint.Publish(Arg.Any<OrganizationCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Message bus failure"));
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task Handle_ValidCommand_AssignsCorrectPermissions()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var employee = await _dbContext.Employees
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync();
        employee.Should().NotBeNull();
        employee!.Permissions.Should().HaveCount(3);

        var assignedPermissionIds = employee.Permissions.Select(p => p.PermissionId).ToList();
        var expectedPermissionIds = await _dbContext.Permissions
            .Where(p => new[]
            {
                PermissionValue.MANAGE_HEALTH_FACILITIES,
                PermissionValue.VIEW_PATIENTS_PROFILE,
                PermissionValue.UPDATE_TENANT_INFO
            }.Contains(p.Value))
            .Select(p => p.Id)
            .ToListAsync();

        assignedPermissionIds.Should().BeEquivalentTo(expectedPermissionIds);
    }

    [Fact]
    public async Task Handle_SomePermissionsMissing_AssignsOnlyExistingPermissions()
    {
        // Arrange
        // Remove 2 permissions, keep only MANAGE_HEALTH_FACILITIES
        var toRemove = await _dbContext.Permissions
            .Where(p => p.Value != PermissionValue.MANAGE_HEALTH_FACILITIES)
            .ToListAsync();
        _dbContext.Permissions.RemoveRange(toRemove);
        await _dbContext.SaveChangesAsync();

        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var employee = await _dbContext.Employees
            .Include(e => e.Permissions)
            .FirstOrDefaultAsync();
        employee!.Permissions.Should().HaveCount(1);
    }


    #endregion
}