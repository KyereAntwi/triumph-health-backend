namespace Triumph.HealthMS.Application.Features.Patients.StartAConsultation;

public sealed class StartAConsultationCommandHandler(
    ITenantContext tenantContext,
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext,
    ILogger<StartAConsultationCommandHandler> logger
)
: IRequestHandler<StartAConsultationCommand, BaseResponse<Unit>>
{
    public async Task<BaseResponse<Unit>> Handle(StartAConsultationCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.UPDATE_CONSULTATION_NOTES, cancellationToken))
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Forbidden: You do not have permission to process consultations."]
            };
        }

        var validation = new StartAConsultationCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Validation Failed",
                StatusCode = 400,
                Errors = validationResult.Errors.Select(e => e.ErrorMessage)
            };
        }

        var existingCunsultation = await dbContext.Consultations
            .FirstOrDefaultAsync(c => c.Id == command.ConsultationId, cancellationToken);

        if (existingCunsultation is null)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Consultation not found",
                StatusCode = 404,
                Errors = ["The specified consultation does not exist."]
            };
        }

        var employee = await dbContext
        .Employees
        .Select(e => new { e.ApplicationUser!.UserId, e.Id })
        .FirstOrDefaultAsync(e => e.UserId == tenantContext.UserId, cancellationToken);

        if (existingCunsultation.DoctorId != employee?.Id)
        {
            logger.LogWarning(
                "Attempt to start consultation {ConsultationId} by user {UserId} who is not the assigned doctor",
                command.ConsultationId,
                tenantContext.UserId
            );
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Forbidden: You can only start consultations assigned to you."]
            };
        }

        if (existingCunsultation.StartedAt != DateTime.MinValue)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Forbidden: This consultation has already been started."]
            };
        }

        logger.LogInformation(
            "Attempt to start consultation {ConsultationId} by user {UserId}",
            command.ConsultationId,
            tenantContext.UserId
        );
        existingCunsultation.StartedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<Unit>
        {
            IsSuccess = true,
            Message = "Consultation started successfully",
            StatusCode = 200
        };
    }
}
