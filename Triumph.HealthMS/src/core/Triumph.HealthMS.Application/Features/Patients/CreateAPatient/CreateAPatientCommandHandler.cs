namespace Triumph.HealthMS.Application.Features.Patients.CreateAPatient;

public sealed class CreateAPatientCommandHandler(
    IApplicationDbContext dbContext,
    ITenantContext tenantContext,
    IPermissionsServices permissionsServices,
    ILogger<CreateAPatientCommandHandler> logger,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<CreateAPatientCommand, BaseResponse<CreateAPatientResponse>>
{
    public async Task<BaseResponse<CreateAPatientResponse>> Handle(CreateAPatientCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, cancellationToken))
        {
            return new BaseResponse<CreateAPatientResponse>
            {
                StatusCode = 403,
                Message = "Forbidden",
                Errors = ["Forbidden! You do not have permission to use this command."],
                IsSuccess = false
            };
        }

        var validation = new CreateAPatientCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<CreateAPatientResponse>
            {
                StatusCode = 400,
                IsSuccess = false,
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray(),
                Message = "Validation Failed"
            };
        }

        // create an application account
        var newApplicationUser = new ApplicationUser
        {
            UserId = "Unassigned",
            FirstName = command.FirstName,
            LastName = command.LastName,
            OtherNames = command.OtherNames ?? string.Empty,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            DateOfBirth = command.DateOfBirth,
            Gender = Enum.Parse<Gender>(command.Gender),
            Nationality = command.Nationality
        };
        await dbContext.ApplicationUsers.AddAsync(newApplicationUser, cancellationToken);

        // create a patient
        var newPatient = new Patient
        {
            ApplicationUserId = newApplicationUser.Id,
            FacilityId = tenantContext.FacilityId ?? Guid.NewGuid(),
            PassportNumber = command.PassportNumber,
            NationalIdNumber = command.NationalIdNumber,
            HomeAddress = command.HomeAddress,
            UniqueIdentifier = $"PT-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
        };
        await dbContext.Patients.AddAsync(newPatient, cancellationToken);

        // create a visit
        var newVisit = new Visit
        {
            PatientId = newPatient.Id,
            VisitReasons = command.VisitReason ?? "Initial Registration",
            VisitTimeStamp = command.VisitTimeStamp ?? DateTime.UtcNow
        };
        await dbContext.Visits.AddAsync(newVisit, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (command.SendLinkForPatientAccountLinking)
            await PublishPatientCreatedEvent(newPatient.Id, cancellationToken);

        return new BaseResponse<CreateAPatientResponse>
        {
            StatusCode = 201,
            Message = "Patient created successfully.",
            Data = new CreateAPatientResponse(newPatient.Id),
            IsSuccess = true
        };
    }

    private async Task PublishPatientCreatedEvent(Guid id, CancellationToken cancellationToken)
    {
        var @event = new PatientCreatedEvent
        {
            CreatedBy = tenantContext.UserId,
            Message = $"A new patient with ID {id} has been created.",
            ResourceType = nameof(Patient),
            ResourceTypeId = id,
            PatientId = id
        };

        try
        {
            await publishEndpoint.Publish(@event, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish PatientCreatedEvent for patient with ID {PatientId} with payload {Event}", id, @event);
        }
    }
}