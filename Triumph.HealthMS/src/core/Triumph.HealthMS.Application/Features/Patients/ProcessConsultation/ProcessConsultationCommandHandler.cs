namespace Triumph.HealthMS.Application.Features.Patients.ProcessConsultation;

public sealed class ProcessConsultationCommandHandler(
    ITenantContext tenantContext,
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext,
    ILogger<ProcessConsultationCommandHandler> logger
)
: IRequestHandler<ProcessConsultationCommand, BaseResponse<Unit>>
{
    public async Task<BaseResponse<Unit>> Handle(ProcessConsultationCommand command, CancellationToken cancellationToken)
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

        var validation = new ProcessConsultationCommandValidator();
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
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Forbidden: You can only process consultations assigned to you."]
            };
        }

        if (existingCunsultation.StartedAt == DateTime.MinValue || existingCunsultation.EndedAt != DateTime.MinValue)
        {
            logger.LogWarning(
                "Attempt to process consultation {ConsultationId} that has not been started or has already been processed by user {UserId}",
                command.ConsultationId,
                tenantContext.UserId
            );

            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Conflict",
                StatusCode = 409,
                Errors = ["This consultation has not been started or has already been processed."]
            };
        }

        existingCunsultation.Notes = command.Notes;
        existingCunsultation.EndedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<Unit>
        {
            IsSuccess = true,
            Message = "Consultation processed successfully",
            StatusCode = 200,
            Data = Unit.Value
        };
    }
}
