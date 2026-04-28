namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class ProcessConsultation : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly ITenantContext _tenantContext;
    private readonly TestAppDbContext _dbContext;
    private readonly ILogger<ProcessConsultationCommandHandler> _logger;
    private readonly ProcessConsultationCommandHandler _sut;
    private readonly Patient _seededPatient;
    private readonly Employee _seededDoctor;
    private readonly ApplicationUser _doctorUser;
    private readonly Consultation _seededConsultation;

    public ProcessConsultation()
    {
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.UPDATE_CONSULTATION_NOTES);
        _dbContext = MockDbContextFactory.Create();
        _logger = Substitute.For<ILogger<ProcessConsultationCommandHandler>>();
        _seededPatient = TestDataSeeder.SeedPatient(_dbContext);

        var role = TestDataSeeder.SeedRole(_dbContext);
        var (employee, user, _) = TestDataSeeder.SeedEmployeeWithRole(_dbContext, role.Id);
        _seededDoctor = employee;
        _doctorUser = user;

        _tenantContext = TestDataSeeder.CreateMockTenantContext(userId: _doctorUser.UserId);

        _seededConsultation = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            EndedAt = DateTime.MinValue
        };
        _dbContext.Consultations.Add(_seededConsultation);
        _dbContext.SaveChanges();

        _sut = new ProcessConsultationCommandHandler(_tenantContext, _permissionsServices, _dbContext, _logger);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private ProcessConsultationCommand CreateValidCommand(
        Guid? consultationId = null,
        string notes = "Patient presented with mild symptoms. Prescribed rest.") =>
        new(
            ConsultationId: consultationId ?? _seededConsultation.Id,
            Notes: notes);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Consultation processed successfully");
        result.Data.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesNotes()
    {
        const string notes = "Diagnosis: common cold. Follow up in 7 days.";

        await _sut.Handle(CreateValidCommand(notes: notes), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.Notes.Should().Be(notes);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsEndedAt()
    {
        var before = DateTime.UtcNow;

        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.EndedAt.Should().BeOnOrAfter(before);
        consultation.EndedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.UPDATE_CONSULTATION_NOTES, granted: false);
        var sut = new ProcessConsultationCommandHandler(_tenantContext, denied, _dbContext, _logger);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Forbidden");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotModifyConsultation()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.UPDATE_CONSULTATION_NOTES, granted: false);
        var sut = new ProcessConsultationCommandHandler(_tenantContext, denied, _dbContext, _logger);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.Notes.Should().BeNull();
        consultation.EndedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyConsultationId_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(consultationId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_EmptyNotes_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(notes: ""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_NotesExceed1000Characters_ReturnsValidationFailure()
    {
        var longNotes = new string('x', 1001);

        var result = await _sut.Handle(CreateValidCommand(notes: longNotes), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_NotesExactly1000Characters_ReturnsSuccess()
    {
        var maxNotes = new string('x', 1000);

        var result = await _sut.Handle(CreateValidCommand(notes: maxNotes), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotModifyConsultation()
    {
        await _sut.Handle(CreateValidCommand(notes: ""), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.Notes.Should().BeNull();
        consultation.EndedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region Consultation Not Found

    [Fact]
    public async Task Handle_NonExistentConsultation_ReturnsNotFound()
    {
        var result = await _sut.Handle(CreateValidCommand(consultationId: Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be("Consultation not found");
        result.Errors.Should().Contain("The specified consultation does not exist.");
    }

    #endregion

    #region Doctor Ownership

    [Fact]
    public async Task Handle_DifferentDoctor_ReturnsForbidden()
    {
        var otherTenantContext = TestDataSeeder.CreateMockTenantContext(userId: "other-user-id");
        var sut = new ProcessConsultationCommandHandler(otherTenantContext, _permissionsServices, _dbContext, _logger);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().Contain("Forbidden: You can only process consultations assigned to you.");
    }

    [Fact]
    public async Task Handle_DifferentDoctor_DoesNotModifyConsultation()
    {
        var otherTenantContext = TestDataSeeder.CreateMockTenantContext(userId: "other-user-id");
        var sut = new ProcessConsultationCommandHandler(otherTenantContext, _permissionsServices, _dbContext, _logger);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.Notes.Should().BeNull();
        consultation.EndedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region Conflict — Not Started or Already Processed

    [Fact]
    public async Task Handle_ConsultationNotStarted_ReturnsConflict()
    {
        var notStarted = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = DateTime.MinValue,
            EndedAt = DateTime.MinValue
        };
        _dbContext.Consultations.Add(notStarted);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.Handle(CreateValidCommand(consultationId: notStarted.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be("Conflict");
        result.Errors.Should().Contain("This consultation has not been started or has already been processed.");
    }

    [Fact]
    public async Task Handle_ConsultationAlreadyProcessed_ReturnsConflict()
    {
        var alreadyProcessed = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        _dbContext.Consultations.Add(alreadyProcessed);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.Handle(CreateValidCommand(consultationId: alreadyProcessed.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be("Conflict");
    }

    [Fact]
    public async Task Handle_ConflictState_DoesNotModifyConsultation()
    {
        var notStarted = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = DateTime.MinValue,
            EndedAt = DateTime.MinValue,
            Notes = null
        };
        _dbContext.Consultations.Add(notStarted);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await _sut.Handle(CreateValidCommand(consultationId: notStarted.Id), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(notStarted.Id);
        consultation!.Notes.Should().BeNull();
        consultation.EndedAt.Should().Be(DateTime.MinValue);
    }

    #endregion
}
