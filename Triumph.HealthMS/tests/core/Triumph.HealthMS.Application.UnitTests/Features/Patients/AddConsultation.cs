namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class AddConsultation : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly TestAppDbContext _dbContext;
    private readonly AddConsultationCommandHandler _sut;
    private readonly Patient _seededPatient;
    private readonly Employee _seededDoctor;
    private readonly Guid _visitId;

    public AddConsultation()
    {
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.CREATE_CONSULTATION);
        _dbContext = MockDbContextFactory.Create();
        _seededPatient = TestDataSeeder.SeedPatient(_dbContext);

        var role = TestDataSeeder.SeedRole(_dbContext);
        var (employee, _, _) = TestDataSeeder.SeedEmployeeWithRole(_dbContext, role.Id);
        _seededDoctor = employee;

        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            VisitTimeStamp = DateTime.UtcNow,
            VisitReasons = "Checkup"
        };
        _dbContext.Visits.Add(visit);
        _dbContext.SaveChanges();
        _visitId = visit.Id;

        _sut = new AddConsultationCommandHandler(_permissionsServices, _dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private AddConsultationCommand CreateValidCommand(
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? visitId = null) =>
        new(
            PatientId: patientId ?? _seededPatient.Id,
            DoctorId: doctorId ?? _seededDoctor.Id,
            VisitId: visitId);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Consultation Added");
        result.Data.Should().NotBeNull();
        result.Data.ConsultationId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsConsultation()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FirstOrDefaultAsync();
        consultation.Should().NotBeNull();
        consultation!.PatientId.Should().Be(_seededPatient.Id);
        consultation.DoctorId.Should().Be(_seededDoctor.Id);
    }

    [Fact]
    public async Task Handle_WithVisitId_PersistsAssociatedVisit()
    {
        await _sut.Handle(CreateValidCommand(visitId: _visitId), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FirstOrDefaultAsync();
        consultation.Should().NotBeNull();
        consultation!.AssociatedVisit.Should().Be(_visitId);
    }

    [Fact]
    public async Task Handle_WithoutVisitId_PersistsNullAssociatedVisit()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FirstOrDefaultAsync();
        consultation.Should().NotBeNull();
        consultation!.AssociatedVisit.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsExactlyOneConsultation()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(1);
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.CREATE_CONSULTATION, granted: false);
        var sut = new AddConsultationCommandHandler(denied, _dbContext);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Forbidden");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotPersist()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.CREATE_CONSULTATION, granted: false);
        var sut = new AddConsultationCommandHandler(denied, _dbContext);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyDoctorId_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(doctorId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_EmptyPatientId_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(patientId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_BothIdsEmpty_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(
            CreateValidCommand(patientId: Guid.Empty, doctorId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Count().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersist()
    {
        await _sut.Handle(CreateValidCommand(patientId: Guid.Empty), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Patient Not Found

    [Fact]
    public async Task Handle_NonExistentPatient_ReturnsNotFound()
    {
        var result = await _sut.Handle(CreateValidCommand(patientId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain("Patient not found.");
    }

    [Fact]
    public async Task Handle_NonExistentPatient_DoesNotPersist()
    {
        await _sut.Handle(CreateValidCommand(patientId: Guid.NewGuid()), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Doctor Not Found

    [Fact]
    public async Task Handle_NonExistentDoctor_ReturnsNotFound()
    {
        var result = await _sut.Handle(CreateValidCommand(doctorId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain("Doctor not found.");
    }

    [Fact]
    public async Task Handle_NonExistentDoctor_DoesNotPersist()
    {
        await _sut.Handle(CreateValidCommand(doctorId: Guid.NewGuid()), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Visit Not Found

    [Fact]
    public async Task Handle_NonExistentVisit_ReturnsNotFound()
    {
        var result = await _sut.Handle(CreateValidCommand(visitId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain("Associated Visit not found.");
    }

    [Fact]
    public async Task Handle_NonExistentVisit_DoesNotPersist()
    {
        await _sut.Handle(CreateValidCommand(visitId: Guid.NewGuid()), CancellationToken.None);

        (await _dbContext.Consultations.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_EmptyGuidVisitId_SkipsVisitLookup_ReturnsSuccess()
    {
        var result = await _sut.Handle(CreateValidCommand(visitId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    #endregion
}
