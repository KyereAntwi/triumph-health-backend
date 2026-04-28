namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class StartAConsultation : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly ITenantContext _tenantContext;
    private readonly TestAppDbContext _dbContext;
    private readonly ILogger<StartAConsultationCommandHandler> _logger;
    private readonly StartAConsultationCommandHandler _sut;
    private readonly Patient _seededPatient;
    private readonly Employee _seededDoctor;
    private readonly ApplicationUser _doctorUser;
    private readonly Consultation _seededConsultation;

    public StartAConsultation()
    {
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.UPDATE_CONSULTATION_NOTES);
        _dbContext = MockDbContextFactory.Create();
        _logger = Substitute.For<ILogger<StartAConsultationCommandHandler>>();
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
            StartedAt = DateTime.MinValue,
            EndedAt = DateTime.MinValue
        };
        _dbContext.Consultations.Add(_seededConsultation);
        _dbContext.SaveChanges();

        _sut = new StartAConsultationCommandHandler(_tenantContext, _permissionsServices, _dbContext, _logger);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private StartAConsultationCommand CreateValidCommand(Guid? consultationId = null) =>
        new(ConsultationId: consultationId ?? _seededConsultation.Id);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Consultation started successfully");
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsStartedAt()
    {
        var before = DateTime.UtcNow;

        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.StartedAt.Should().BeOnOrAfter(before);
        consultation.StartedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_DoesNotSetEndedAt()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.EndedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.UPDATE_CONSULTATION_NOTES, granted: false);
        var sut = new StartAConsultationCommandHandler(_tenantContext, denied, _dbContext, _logger);

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
        var sut = new StartAConsultationCommandHandler(_tenantContext, denied, _dbContext, _logger);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.StartedAt.Should().Be(DateTime.MinValue);
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
    public async Task Handle_ValidationFails_DoesNotModifyConsultation()
    {
        await _sut.Handle(CreateValidCommand(consultationId: Guid.Empty), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.StartedAt.Should().Be(DateTime.MinValue);
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
        var sut = new StartAConsultationCommandHandler(otherTenantContext, _permissionsServices, _dbContext, _logger);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().Contain("Forbidden: You can only start consultations assigned to you.");
    }

    [Fact]
    public async Task Handle_DifferentDoctor_DoesNotModifyConsultation()
    {
        var otherTenantContext = TestDataSeeder.CreateMockTenantContext(userId: "other-user-id");
        var sut = new StartAConsultationCommandHandler(otherTenantContext, _permissionsServices, _dbContext, _logger);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(_seededConsultation.Id);
        consultation!.StartedAt.Should().Be(DateTime.MinValue);
    }

    #endregion

    #region Already Started

    [Fact]
    public async Task Handle_ConsultationAlreadyStarted_ReturnsForbidden()
    {
        var alreadyStarted = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = DateTime.UtcNow.AddMinutes(-10),
            EndedAt = DateTime.MinValue
        };
        _dbContext.Consultations.Add(alreadyStarted);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.Handle(CreateValidCommand(consultationId: alreadyStarted.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().Contain("Forbidden: This consultation has already been started.");
    }

    [Fact]
    public async Task Handle_ConsultationAlreadyStarted_DoesNotModifyStartedAt()
    {
        var originalStartedAt = DateTime.UtcNow.AddMinutes(-10);
        var alreadyStarted = new Consultation
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            DoctorId = _seededDoctor.Id,
            StartedAt = originalStartedAt,
            EndedAt = DateTime.MinValue
        };
        _dbContext.Consultations.Add(alreadyStarted);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await _sut.Handle(CreateValidCommand(consultationId: alreadyStarted.Id), CancellationToken.None);

        var consultation = await _dbContext.Consultations.FindAsync(alreadyStarted.Id);
        consultation!.StartedAt.Should().Be(originalStartedAt);
    }

    #endregion
}
