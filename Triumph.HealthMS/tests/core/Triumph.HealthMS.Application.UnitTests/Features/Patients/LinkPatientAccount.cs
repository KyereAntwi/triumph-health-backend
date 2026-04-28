namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class LinkPatientAccount : IDisposable
{
    private readonly TestAppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly LinkPatientToCreatedAccountCommandHandler _sut;
    private readonly Patient _seededPatient;

    public LinkPatientAccount()
    {
        _dbContext = MockDbContextFactory.Create();
        _tenantContext = TestDataSeeder.CreateMockTenantContext();
        _seededPatient = TestDataSeeder.SeedPatient(_dbContext);

        _sut = new LinkPatientToCreatedAccountCommandHandler(_tenantContext, _dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private LinkPatientToCreatedAccountCommand CreateValidCommand(Guid? patientId = null) =>
        new(PatientId: patientId ?? _seededPatient.Id);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Patient linked to account successfully");
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesApplicationUserUserId()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == _seededPatient.ApplicationUserId);
        user.Should().NotBeNull();
        user!.UserId.Should().Be(TestConstants.TestUserId);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUnitValue()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.Data.Should().Be(Unit.Value);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyPatientId_ReturnsValidationFailure()
    {
        var command = new LinkPatientToCreatedAccountCommand(Guid.Empty);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation failed");
        result.Errors.Should().Contain(e => e.Contains("PatientId"));
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotModifyUser()
    {
        var command = new LinkPatientToCreatedAccountCommand(Guid.Empty);

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == _seededPatient.ApplicationUserId);
        user!.UserId.Should().Be("Unassigned");
    }

    #endregion

    #region Patient Not Found

    [Fact]
    public async Task Handle_NonExistentPatient_ReturnsNotFound()
    {
        var command = CreateValidCommand(patientId: Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Not found");
        result.Errors.Should().Contain("Patient not found");
    }

    [Fact]
    public async Task Handle_NonExistentPatient_DoesNotModifyAnyUser()
    {
        var command = CreateValidCommand(patientId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == _seededPatient.ApplicationUserId);
        user!.UserId.Should().Be("Unassigned");
    }

    #endregion

    #region Tenant Context

    [Fact]
    public async Task Handle_DifferentUserId_LinksCorrectUserId()
    {
        var customTenantCtx = TestDataSeeder.CreateMockTenantContext(userId: "custom-user-456");
        var sut = new LinkPatientToCreatedAccountCommandHandler(customTenantCtx, _dbContext);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == _seededPatient.ApplicationUserId);
        user!.UserId.Should().Be("custom-user-456");
    }

    [Fact]
    public async Task Handle_CalledTwice_OverwritesWithLatestUserId()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var secondCtx = TestDataSeeder.CreateMockTenantContext(userId: "second-user");
        var secondSut = new LinkPatientToCreatedAccountCommandHandler(secondCtx, _dbContext);
        await secondSut.Handle(CreateValidCommand(), CancellationToken.None);

        var user = await _dbContext.ApplicationUsers
            .FirstOrDefaultAsync(u => u.Id == _seededPatient.ApplicationUserId);
        user!.UserId.Should().Be("second-user");
    }

    #endregion
}