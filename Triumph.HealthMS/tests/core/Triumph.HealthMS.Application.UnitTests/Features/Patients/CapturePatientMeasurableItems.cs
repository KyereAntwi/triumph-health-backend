namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class CapturePatientMeasurableItems : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly TestAppDbContext _dbContext;
    private readonly CapturePatientMeasurableItemsCommandHandler _sut;
    private readonly Patient _seededPatient;
    private readonly Guid _visitId;
    private readonly Guid _opdItemId;

    public CapturePatientMeasurableItems()
    {
        _permissionsServices = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_MEASUREMENTS);
        _dbContext = MockDbContextFactory.Create();
        _seededPatient = TestDataSeeder.SeedPatient(_dbContext);

        // Seed a visit for the patient
        var visit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = _seededPatient.Id,
            VisitTimeStamp = DateTime.UtcNow,
            VisitReasons = "Checkup"
        };
        _dbContext.Visits.Add(visit);
        _visitId = visit.Id;

        // Seed a FacilityOpdCaptureItem
        _opdItemId = Guid.NewGuid();
        _dbContext.FacilityOpdCaptureItems.Add(new FacilityOpdCaptureItem
        {
            Id = _opdItemId,
            FacilityId = _seededPatient.FacilityId,
            OpdCaptureItemId = Guid.NewGuid()
        });
        _dbContext.SaveChanges();

        _sut = new CapturePatientMeasurableItemsCommandHandler(_permissionsServices, _dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private CapturePatientMeasurableItemsCommand CreateValidCommand(
        Guid? patientId = null,
        Guid? visitId = null,
        IEnumerable<OpdItemCommandDto>? opdItems = null) =>
        new(
            PatientId: patientId ?? _seededPatient.Id,
            VisitId: visitId ?? _visitId,
            OpdItems: opdItems ?? [new OpdItemCommandDto(_opdItemId, "120/80", "Normal")]);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var result = await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("Operation Completed");
        result.Data.Should().Be(Unit.Value);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsMeasurement()
    {
        await _sut.Handle(CreateValidCommand(), CancellationToken.None);

        var item = await _dbContext.PatientFacilityOpdCaptureItems.FirstOrDefaultAsync();
        item.Should().NotBeNull();
        item!.PatientId.Should().Be(_seededPatient.Id);
        item.FacilityOpdCaptureItemId.Should().Be(_opdItemId);
        item.ValueCaptured.Should().Be("120/80");
        item.Notes.Should().Be("Normal");
    }

    [Fact]
    public async Task Handle_MultipleOpdItems_PersistsAll()
    {
        var secondOpdItemId = Guid.NewGuid();
        _dbContext.FacilityOpdCaptureItems.Add(new FacilityOpdCaptureItem
        {
            Id = secondOpdItemId,
            FacilityId = _seededPatient.FacilityId,
            OpdCaptureItemId = Guid.NewGuid()
        });
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var items = new[]
        {
            new OpdItemCommandDto(_opdItemId, "120/80", "BP"),
            new OpdItemCommandDto(secondOpdItemId, "36.5", "Temp")
        };

        await _sut.Handle(CreateValidCommand(opdItems: items), CancellationToken.None);

        (await _dbContext.PatientFacilityOpdCaptureItems.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Handle_ExistingMeasurementWithMatchingVisit_UpdatesInsteadOfCreating()
    {
        // Seed an existing measurement with AssociatedVisit set
        _dbContext.PatientFacilityOpdCaptureItems.Add(new PatientFacilityOpdCaptureItem
        {
            PatientId = _seededPatient.Id,
            FacilityOpdCaptureItemId = _opdItemId,
            AssociatedVisit = _visitId,
            ValueCaptured = "120/80",
            Notes = "Normal"
        });
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Update with new values
        var updatedItems = new[] { new OpdItemCommandDto(_opdItemId, "130/90", "Elevated") };
        await _sut.Handle(CreateValidCommand(opdItems: updatedItems), CancellationToken.None);

        (await _dbContext.PatientFacilityOpdCaptureItems.CountAsync()).Should().Be(1);
        var item = await _dbContext.PatientFacilityOpdCaptureItems.FirstAsync();
        item.ValueCaptured.Should().Be("130/90");
        item.Notes.Should().Be("Elevated");
    }

    [Fact]
    public async Task Handle_NullNotes_PersistsEmptyString()
    {
        var items = new[] { new OpdItemCommandDto(_opdItemId, "120/80", null) };

        await _sut.Handle(CreateValidCommand(opdItems: items), CancellationToken.None);

        var item = await _dbContext.PatientFacilityOpdCaptureItems.FirstAsync();
        item.Notes.Should().BeEmpty();
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbidden()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE, granted: false);
        var sut = new CapturePatientMeasurableItemsCommandHandler(denied, _dbContext);

        var result = await sut.Handle(CreateValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotPersist()
    {
        var denied = TestDataSeeder.CreateMockPermissionsService(
            PermissionValue.MANAGE_PATIENTS_PROFILE, granted: false);
        var sut = new CapturePatientMeasurableItemsCommandHandler(denied, _dbContext);

        await sut.Handle(CreateValidCommand(), CancellationToken.None);

        (await _dbContext.PatientFacilityOpdCaptureItems.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_EmptyPatientId_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(patientId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_EmptyVisitId_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(visitId: Guid.Empty), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyOpdItems_ReturnsValidationFailure()
    {
        var result = await _sut.Handle(CreateValidCommand(opdItems: []), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_OpdItemWithEmptyValue_ReturnsValidationFailure()
    {
        var items = new[] { new OpdItemCommandDto(_opdItemId, "", "Note") };

        var result = await _sut.Handle(CreateValidCommand(opdItems: items), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_OpdItemWithEmptyItemId_ReturnsValidationFailure()
    {
        var items = new[] { new OpdItemCommandDto(Guid.Empty, "120/80", null) };

        var result = await _sut.Handle(CreateValidCommand(opdItems: items), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersist()
    {
        await _sut.Handle(CreateValidCommand(patientId: Guid.Empty), CancellationToken.None);

        (await _dbContext.PatientFacilityOpdCaptureItems.CountAsync()).Should().Be(0);
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

        (await _dbContext.PatientFacilityOpdCaptureItems.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Visit Not Found

    [Fact]
    public async Task Handle_NonExistentVisit_ReturnsNotFound()
    {
        var badVisitId = Guid.NewGuid();
        var result = await _sut.Handle(CreateValidCommand(visitId: badVisitId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain(e => e.Contains(badVisitId.ToString()));
    }

    [Fact]
    public async Task Handle_VisitBelongsToDifferentPatient_ReturnsNotFound()
    {
        var otherPatient = TestDataSeeder.SeedPatient(_dbContext);
        var otherVisit = new Visit
        {
            Id = Guid.NewGuid(),
            PatientId = otherPatient.Id,
            VisitTimeStamp = DateTime.UtcNow,
            VisitReasons = "Other"
        };
        _dbContext.Visits.Add(otherVisit);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.Handle(
            CreateValidCommand(visitId: otherVisit.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    #endregion

    #region OPD Items Not Found

    [Fact]
    public async Task Handle_NonExistentOpdItem_ReturnsNotFound()
    {
        var items = new[] { new OpdItemCommandDto(Guid.NewGuid(), "120/80", null) };

        var result = await _sut.Handle(CreateValidCommand(opdItems: items), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Errors.Should().Contain("One or more of the provided FacilityOpdCaptureItemIds were not found.");
    }

    #endregion
}