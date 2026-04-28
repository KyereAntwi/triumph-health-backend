namespace Triumph.HealthMS.Application.UnitTests.Features.HealthFacilities;

public class CreateAFacility : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateAFacilityCommandHandler> _logger;
    private readonly TestAppDbContext _dbContext;
    private readonly CreateAFacilityCommandHandler _sut;

    public CreateAFacility()
    {
        _permissionsServices = Substitute.For<IPermissionsServices>();
        _tenantContext = TestDataSeeder.CreateMockTenantContext();
        _logger = Substitute.For<ILogger<CreateAFacilityCommandHandler>>();
        _dbContext = MockDbContextFactory.Create();

        // Default: user has permission
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, Arg.Any<CancellationToken>())
            .Returns(true);

        _sut = new CreateAFacilityCommandHandler(
            _permissionsServices,
            _dbContext,
            _logger,
            _tenantContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private static CreateAFacilityCommand CreateValidCommand() =>
        new(
            Name: "City General Hospital",
            Address: "456 Health Avenue",
            Location: "Downtown District",
            Email: "facility@test.com",
            MainPhone: "+1234567890");

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
        result.Message.Should().Be("Success");
        result.Data.Should().NotBeNull();
        result.Data.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsFacilityToDatabase()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var facility = await _dbContext.HealthFacilities.FirstOrDefaultAsync();
        facility.Should().NotBeNull();
        facility!.Name.Should().Be(command.Name);
        facility.Address.Should().Be(command.Address);
        facility.Location.Should().Be(command.Location);
        facility.Email.Should().Be(command.Email);
        facility.MainPhone.Should().Be(command.MainPhone);
    }

    [Fact]
    public async Task Handle_ValidCommand_AssignsTenantIdFromContext()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        var facility = await _dbContext.HealthFacilities.FirstOrDefaultAsync();
        facility.Should().NotBeNull();
        facility!.TenantId.Should().Be(TestConstants.TestTenantId);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnedIdMatchesPersisted()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        var facility = await _dbContext.HealthFacilities.FindAsync(result.Data.Id);
        facility.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesExactlyOneFacility()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChecksPermission()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _permissionsServices.Received(1)
            .HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsUnauthorizedResponse()
    {
        // Arrange
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Unauthorized");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotCreateFacility()
    {
        // Arrange
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoPermission_SkipsValidationAndDbCheck()
    {
        // Arrange — no permission + invalid data: should return 403, not 400
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand() with { Name = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.StatusCode.Should().Be(403);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyName_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_NameExceedsMaxLength_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 101) };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
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
    }

    [Fact]
    public async Task Handle_AddressExceedsMaxLength_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Address = new string('A', 101) };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyLocation_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Location = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_LocationExceedsMaxLength_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Location = new string('A', 101) };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ReturnsValidationFailure()
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
    public async Task Handle_EmptyMainPhone_ReturnsValidationFailure()
    {
        // Arrange
        var command = CreateValidCommand() with { MainPhone = "" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidPhoneFormat_ReturnsValidationFailure()
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
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Name = "",
            Address = "",
            Location = "",
            Email = "bad",
            MainPhone = "bad"
        };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersistFacility()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = "" };

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Duplicate Name (Conflict)

    [Fact]
    public async Task Handle_DuplicateFacilityName_ReturnsConflictResponse()
    {
        // Arrange
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = "City General Hospital",
            Address = "Existing Address",
            Location = "Existing Location",
            Email = "existing@test.com",
            MainPhone = "+9876543210",
            TenantId = TestConstants.TestTenantId
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be("Conflict");
        result.Errors.Should().Contain(e => e.Contains("same name"));
    }

    [Fact]
    public async Task Handle_DuplicateNameCaseInsensitive_ReturnsConflictResponse()
    {
        // Arrange
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = "city general hospital", // lowercase
            Address = "Existing Address",
            Location = "Existing Location",
            Email = "existing@test.com",
            MainPhone = "+9876543210",
            TenantId = TestConstants.TestTenantId
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand(); // Name = "City General Hospital"

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_DuplicateName_DoesNotCreateSecondFacility()
    {
        // Arrange
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = "City General Hospital",
            Address = "Existing Address",
            Location = "Existing Location",
            Email = "existing@test.com",
            MainPhone = "+9876543210",
            TenantId = TestConstants.TestTenantId
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_DifferentFacilityName_AllowsCreation()
    {
        // Arrange
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = "Different Hospital",
            Address = "Existing Address",
            Location = "Existing Location",
            Email = "existing@test.com",
            MainPhone = "+9876543210",
            TenantId = TestConstants.TestTenantId
        });
        await _dbContext.SaveChangesAsync();
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_PhoneNumberWithPlus_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand() with { MainPhone = "+233244123456" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_PhoneNumberWithoutPlus_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand() with { MainPhone = "1234567890" };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_NameAtMaxLength_Succeeds()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 100) };

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_MultipleFacilitiesWithDifferentNames_CreatesAll()
    {
        // Arrange & Act
        var result1 = await _sut.Handle(CreateValidCommand() with { Name = "Facility One" }, CancellationToken.None);
        var result2 = await _sut.Handle(CreateValidCommand() with { Name = "Facility Two" }, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        (await _dbContext.HealthFacilities.CountAsync()).Should().Be(2);
        result1.Data.Id.Should().NotBe(result2.Data.Id);
    }

    [Fact]
    public async Task Handle_SameEmailDifferentName_AllowsCreation()
    {
        // Arrange — duplicate email is allowed as long as name differs
        _dbContext.HealthFacilities.Add(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = "Other Hospital",
            Address = "Other Address",
            Location = "Other Location",
            Email = "facility@test.com", // same email as valid command
            MainPhone = "+9876543210",
            TenantId = TestConstants.TestTenantId
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
}