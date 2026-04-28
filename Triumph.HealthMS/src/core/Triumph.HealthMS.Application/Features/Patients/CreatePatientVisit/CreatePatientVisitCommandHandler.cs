namespace Triumph.HealthMS.Application.Features.Patients.CreatePatientVisit;

public sealed class CreatePatientVisitCommandHandler(
    IApplicationDbContext dbContext,
    IPermissionsServices permissionsServices) 
    : IRequestHandler<CreatePatientVisitCommand, BaseResponse<CreatePatientVisitResponse>>
{
    public async Task<BaseResponse<CreatePatientVisitResponse>> Handle(CreatePatientVisitCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANAGE_PATIENTS_PROFILE, cancellationToken))
        {
            return new BaseResponse<CreatePatientVisitResponse>
            {
                IsSuccess = false,
                StatusCode = 403,
                Message = "Forbidden",
                Errors = ["Forbidden! You do not have permission to use this command."]
            };
        }

        var validation = new CreatePatientVisitCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<CreatePatientVisitResponse>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Validation Failed",
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };
        }
        
        var existingPatient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

        if (existingPatient is null)
        {
            return new BaseResponse<CreatePatientVisitResponse>
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = "Not Found",
                Errors = ["Patient not found"]
            };
        }
        
        var existingVisit = await dbContext.Visits.Where(v => v.PatientId == command.PatientId && v.CreatedAt.Date == DateTime.UtcNow.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingVisit is not null)
        {
            return new BaseResponse<CreatePatientVisitResponse>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Bad Request",
                Errors = ["A visit for this patient already exists for today"]
            };
        }

        var newVisit = new Visit
        {
            PatientId = command.PatientId,
            VisitTimeStamp = DateTime.UtcNow,
            VisitReasons = command.VisitReason
        };
        
        await dbContext.Visits.AddAsync(newVisit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<CreatePatientVisitResponse>
        {
            IsSuccess = true,
            StatusCode = 201,
            Message = "Success",
            Data = new CreatePatientVisitResponse(newVisit.Id)
        };
    }
}