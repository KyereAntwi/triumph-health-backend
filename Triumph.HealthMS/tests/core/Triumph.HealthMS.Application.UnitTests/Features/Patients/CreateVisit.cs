namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class CreateVisit : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly TestAppDbContext _dbContext;
    private readonly CreatePatientVisitCommandHandler _sut;
    private readonly Patient _seededPatient;

    public CreateVisit()
    {
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE);
        _dbContext = MockDbContextFactory.Create();
        _seededPatient = TestDataSeeder.SeedPatient(_dbContext);

        _sut = new CreatePatientVisitCommandHandler(_dbContext, _permissionsServices);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private CreatePatientVisitCommand CreateValidCommand(
        Guid? patientId = null,
        string visitReason = "Routine checkup") =>
        new(
            PatientId: patientId ?? _seededPatient.Id,
            VisitReason: visitReason);

    private void SeedVisitForToday(Guid patientId, string reason = "Earlier visit")
    {
        _dbContext.Visits.Add(new Visit
        {
            PatientId = patientId,
            VisitTimeStamp = DateTime.UtcNow,
            VisitReasons = reason,
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Success");
        result.Data.Should().NotBeNull();
        result.Data.VisitId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsVisit()
    {
        var command = CreateValidCommand(visitReason: "Headache");

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.PatientId.Should().Be(_seededPatient.Id);
        visit.VisitReasons.Should().Be("Headache");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsVisitTimeStampToUtcNow()
    {
        var before = DateTime.UtcNow;
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.VisitTimeStamp.Should().BeOnOrAfter(before);
        visit.VisitTimeStamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnedVisitIdMatchesPersisted()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        result.Data.VisitId.Should().Be(visit!.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsExactlyOneVisit()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Visits.CountAsync()).Should().Be(1);
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbiddenResponse()
    {
        var deniedService = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE, granted: false);
        var sut = new CreatePatientVisitCommandHandler(_dbContext, deniedService);
        var command = CreateValidCommand();

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Forbidden");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotCreateVisit()
    {
        var deniedService = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE, granted: false);
        var sut = new CreatePatientVisitCommandHandler(_dbContext, deniedService);
        var command = CreateValidCommand();

        await sut.Handle(command, CancellationToken.None);

        (await _dbContext.Visits.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoPermission_SkipsValidation()
    {
        var deniedService = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE, granted: false);
        var sut = new CreatePatientVisitCommandHandler(_dbContext, deniedService);
        var command = new CreatePatientVisitCommand(Guid.Empty, "");

        var result = await sut.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChecksPermission()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        await _permissionsServices.Received(1)
            .HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyPatientId_ReturnsValidationFailure()
    {
        var command = new CreatePatientVisitCommand(Guid.Empty, "Checkup");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_EmptyVisitReason_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(visitReason: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_BothFieldsEmpty_ReturnsMultipleErrors()
    {
        var command = new CreatePatientVisitCommand(Guid.Empty, "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersistVisit()
    {
        var command = new CreatePatientVisitCommand(Guid.Empty, "");

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Visits.CountAsync()).Should().Be(0);
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
        result.Message.Should().Be("Not Found");
        result.Errors.Should().Contain("Patient not found");
    }

    [Fact]
    public async Task Handle_NonExistentPatient_DoesNotPersistVisit()
    {
        var command = CreateValidCommand(patientId: Guid.NewGuid());

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Visits.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Duplicate Visit Today

    [Fact]
    public async Task Handle_VisitAlreadyExistsToday_ReturnsBadRequest()
    {
        SeedVisitForToday(_seededPatient.Id);
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Bad Request");
        result.Errors.Should().Contain("A visit for this patient already exists for today");
    }

    [Fact]
    public async Task Handle_VisitAlreadyExistsToday_DoesNotCreateSecondVisit()
    {
        SeedVisitForToday(_seededPatient.Id);
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Visits.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_VisitExistsForDifferentPatient_Succeeds()
    {
        var otherPatient = TestDataSeeder.SeedPatient(_dbContext);
        SeedVisitForToday(otherPatient.Id);
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    #endregion
}