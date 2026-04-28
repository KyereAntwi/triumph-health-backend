namespace Triumph.HealthMS.Application.UnitTests.Features.Patients;

public class CreateAPatient : IDisposable
{
    private readonly IPermissionsServices _permissionsServices;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly TestAppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly CreateAPatientCommandHandler _sut;

    private static readonly Guid TestFacilityId = Guid.NewGuid();

    public CreateAPatient()
    {
        _permissionsServices = Substitute.For<IPermissionsServices>();
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _tenantContext = TestDataSeeder.CreateMockTenantContext(facilityId: TestFacilityId);
        var logger = Substitute.For<ILogger<CreateAPatientCommandHandler>>();
        _dbContext = MockDbContextFactory.Create();

        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, Arg.Any<CancellationToken>())
            .Returns(true);

        _sut = new CreateAPatientCommandHandler(
            _dbContext,
            _tenantContext,
            _permissionsServices,
            logger,
            _publishEndpoint);
    }

    public void Dispose() => _dbContext.Dispose();

    #region Helpers

    private static CreateAPatientCommand CreateValidCommand(
        string firstName = "John",
        string lastName = "Doe",
        string otherNames = "Middle",
        string? email = "john.doe@test.com",
        string phoneNumber = "+1234567890",
        string gender = "Male",
        DateOnly? dateOfBirth = null,
        string nationality = "Ghanaian",
        string? passportNumber = "P12345",
        string? homeAddress = "123 Main St",
        string nationalIdNumber = "GHA-123456",
        string? visitReason = "Checkup",
        DateTime? visitTimeStamp = null,
        bool sendLink = false) =>
        new(
            FirstName: firstName,
            LastName: lastName,
            OtherNames: otherNames,
            Email: email,
            PhoneNumber: phoneNumber,
            Gender: gender,
            DateOfBirth: dateOfBirth ?? new DateOnly(1990, 5, 15),
            Nationality: nationality,
            PassportNumber: passportNumber,
            HomeAddress: homeAddress,
            NationalIdNumber: nationalIdNumber,
            VisitReason: visitReason,
            VisitTimeStamp: visitTimeStamp,
            SendLinkForPatientAccountLinking: sendLink);

    #endregion

    #region Happy Path

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessResponse()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Message.Should().Be("Patient created successfully.");
        result.Data.Should().NotBeNull();
        result.Data.PatientId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesApplicationUser()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.FirstName.Should().Be(command.FirstName);
        user.LastName.Should().Be(command.LastName);
        user.OtherNames.Should().Be(command.OtherNames);
        user.Email.Should().Be(command.Email);
        user.PhoneNumber.Should().Be(command.PhoneNumber);
        user.DateOfBirth.Should().Be(command.DateOfBirth);
        user.Gender.Should().Be(Gender.Male);
        user.Nationality.Should().Be(command.Nationality);
        user.UserId.Should().Be("Unassigned");
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPatient()
    {
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        var patient = await _dbContext.Patients.FirstOrDefaultAsync();
        patient.Should().NotBeNull();
        patient!.Id.Should().Be(result.Data.PatientId);
        patient.PassportNumber.Should().Be(command.PassportNumber);
        patient.NationalIdNumber.Should().Be(command.NationalIdNumber);
        patient.HomeAddress.Should().Be(command.HomeAddress);
        patient.FacilityId.Should().Be(TestFacilityId);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesVisit()
    {
        var command = CreateValidCommand(visitReason: "Flu symptoms");

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.VisitReasons.Should().Be("Flu symptoms");
    }

    [Fact]
    public async Task Handle_NullVisitReason_DefaultsToInitialRegistration()
    {
        var command = CreateValidCommand(visitReason: null);

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.VisitReasons.Should().Be("Initial Registration");
    }

    [Fact]
    public async Task Handle_NullVisitTimeStamp_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var command = CreateValidCommand(visitTimeStamp: null);

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.VisitTimeStamp.Should().BeOnOrAfter(before);
        visit.VisitTimeStamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ProvidedVisitTimeStamp_UsesProvidedValue()
    {
        var ts = new DateTime(2025, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var command = CreateValidCommand(visitTimeStamp: ts);

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        visit!.VisitTimeStamp.Should().Be(ts);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsExactlyOneOfEach()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(1);
        (await _dbContext.Patients.CountAsync()).Should().Be(1);
        (await _dbContext.Visits.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_LinksPatientToApplicationUser()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var patient = await _dbContext.Patients.FirstOrDefaultAsync();
        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        patient.Should().NotBeNull();
        user.Should().NotBeNull();
        patient!.ApplicationUserId.Should().Be(user!.Id);
    }

    [Fact]
    public async Task Handle_ValidCommand_LinksVisitToPatient()
    {
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        var visit = await _dbContext.Visits.FirstOrDefaultAsync();
        var patient = await _dbContext.Patients.FirstOrDefaultAsync();
        visit.Should().NotBeNull();
        patient.Should().NotBeNull();
        visit!.PatientId.Should().Be(patient!.Id);
    }

    [Fact]
    public async Task Handle_NoFacilityIdInTenantContext_UsesFallbackGuid()
    {
        var tenantNoFacility = TestDataSeeder.CreateMockTenantContext(facilityId: null);
        var logger = Substitute.For<ILogger<CreateAPatientCommandHandler>>();
        var sut = new CreateAPatientCommandHandler(
            _dbContext, tenantNoFacility, _permissionsServices, logger, _publishEndpoint);

        var command = CreateValidCommand();
        await sut.Handle(command, CancellationToken.None);

        var patient = await _dbContext.Patients.FirstOrDefaultAsync();
        patient.Should().NotBeNull();
        patient!.FacilityId.Should().NotBeEmpty();
    }

    #endregion

    #region Permission Denied

    [Fact]
    public async Task Handle_NoPermission_ReturnsForbiddenResponse()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be("Forbidden");
        result.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task Handle_NoPermission_DoesNotCreateEntities()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand();

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Patients.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
        (await _dbContext.Visits.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoPermission_SkipsValidation()
    {
        _permissionsServices
            .HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, Arg.Any<CancellationToken>())
            .Returns(false);
        var command = CreateValidCommand(firstName: "");

        var result = await _sut.Handle(command, CancellationToken.None);

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
    public async Task Handle_EmptyFirstName_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(firstName: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Message.Should().Be("Validation Failed");
    }

    [Fact]
    public async Task Handle_FirstNameTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(firstName: new string('A', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyLastName_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(lastName: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_LastNameTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(lastName: new string('B', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_OtherNamesTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(otherNames: new string('C', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(email: "not-an-email");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyPhoneNumber_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(phoneNumber: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_PhoneNumberTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(phoneNumber: new string('1', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_InvalidGender_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(gender: "Invalid");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyGender_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(gender: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyNationality_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(nationality: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_NationalityTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(nationality: new string('N', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_EmptyNationalIdNumber_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(nationalIdNumber: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_NationalIdNumberTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(nationalIdNumber: new string('X', 21));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_PassportNumberTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(passportNumber: new string('P', 16));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_HomeAddressTooLong_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(homeAddress: new string('H', 101));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_SendLinkTrue_EmailRequired_ReturnsValidationFailure()
    {
        var command = CreateValidCommand(email: null, sendLink: true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_MultipleValidationErrors_ReturnsAllErrors()
    {
        var command = CreateValidCommand(
            firstName: "",
            lastName: "",
            phoneNumber: "",
            nationalIdNumber: "");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Handle_ValidationFails_DoesNotPersistEntities()
    {
        var command = CreateValidCommand(firstName: "");

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Patients.CountAsync()).Should().Be(0);
        (await _dbContext.ApplicationUsers.CountAsync()).Should().Be(0);
        (await _dbContext.Visits.CountAsync()).Should().Be(0);
    }

    #endregion

    #region Event Publishing

    [Fact]
    public async Task Handle_SendLinkTrue_PublishesPatientCreatedEvent()
    {
        var command = CreateValidCommand(sendLink: true, email: "john@test.com");

        await _sut.Handle(command, CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(
            Arg.Is<PatientCreatedEvent>(e =>
                e.CreatedBy == TestConstants.TestUserId &&
                e.ResourceType == nameof(Patient)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SendLinkFalse_DoesNotPublishEvent()
    {
        var command = CreateValidCommand(sendLink: false);

        await _sut.Handle(command, CancellationToken.None);

        await _publishEndpoint.DidNotReceive().Publish(
            Arg.Any<PatientCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublishFails_StillReturnsSuccess()
    {
        _publishEndpoint.Publish(Arg.Any<PatientCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Message bus failure"));
        var command = CreateValidCommand(sendLink: true, email: "john@test.com");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_PublishFails_PatientStillPersisted()
    {
        _publishEndpoint.Publish(Arg.Any<PatientCreatedEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Message bus failure"));
        var command = CreateValidCommand(sendLink: true, email: "john@test.com");

        await _sut.Handle(command, CancellationToken.None);

        (await _dbContext.Patients.CountAsync()).Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_NullOptionalFields_CreatesPatientSuccessfully()
    {
        var command = CreateValidCommand(
            email: null,
            passportNumber: null,
            homeAddress: null,
            visitReason: null,
            visitTimeStamp: null,
            sendLink: false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_NullEmail_SendLinkFalse_Succeeds()
    {
        var command = CreateValidCommand(email: null, sendLink: false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FemaleGender_ParsesCorrectly()
    {
        var command = CreateValidCommand(gender: "Female");

        await _sut.Handle(command, CancellationToken.None);

        var user = await _dbContext.ApplicationUsers.FirstOrDefaultAsync();
        user.Should().NotBeNull();
        user!.Gender.Should().Be(Gender.Female);
    }

    #endregion
}

