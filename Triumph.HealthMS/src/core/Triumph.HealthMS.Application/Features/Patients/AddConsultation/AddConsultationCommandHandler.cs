namespace Triumph.HealthMS.Application.Features.Patients.AddConsultation;

public sealed class AddConsultationCommandHandler(
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext) 
    : IRequestHandler<AddConsultationCommand, BaseResponse<AddConsultationResult>>
{
    public async Task<BaseResponse<AddConsultationResult>> Handle(AddConsultationCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.CREATE_CONSULTATION, cancellationToken))
        {
            return new BaseResponse<AddConsultationResult>
            {
                IsSuccess = false,
                Message = "Forbidden",
                Errors = ["You don't have permission to do that."],
                StatusCode = 403
            };
        }

        var validation = new AddConsultationCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            return new BaseResponse<AddConsultationResult>
            {
                IsSuccess = false,
                Message = "Validation Failed",
                Errors = validationResult.Errors.Select(x => x.ErrorMessage),
                StatusCode = 400
            };
        }

        var existingPatient = await dbContext.Patients.FindAsync(command.PatientId, cancellationToken);
        if (existingPatient is null)
        {
            return new BaseResponse<AddConsultationResult>
            {
                IsSuccess = false,
                Message = "Not found",
                Errors = ["Patient not found."],
                StatusCode = 404
            };
        }

        var existingDoctor = await dbContext.Employees.FindAsync(command.DoctorId, cancellationToken);
        if (existingDoctor is null)
        {
            return new BaseResponse<AddConsultationResult>
            {
                IsSuccess = false,
                Message = "Not found",
                Errors = ["Doctor not found."],
                StatusCode = 404
            };
        }

        if (command.VisitId != null && command.VisitId != Guid.Empty)
        {
            var existingVisit = await dbContext.Visits.FindAsync(command.VisitId, cancellationToken);
            if (existingVisit is null)
            {
                return new BaseResponse<AddConsultationResult>
                {
                    IsSuccess = false,
                    Message = "Not found",
                    Errors = ["Associated Visit not found."],
                    StatusCode = 404
                };
            }
        }

        var newConsultation = new Consultation
        {
            PatientId = command.PatientId,
            DoctorId = command.DoctorId,
            AssociatedVisit = command.VisitId
        };
        await dbContext.Consultations.AddAsync(newConsultation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<AddConsultationResult>
        {
            IsSuccess = true,
            Message = "Consultation Added",
            Data = new AddConsultationResult(newConsultation.Id),
            StatusCode = 201
        };
    }
}