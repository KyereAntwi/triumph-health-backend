namespace Triumph.HealthMS.Application.Features.Patients.AddPatientMedications;

public sealed class AddPatientMedicationsCommandHandler(
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext) 
    : IRequestHandler<AddPatientMedicationsCommand, BaseResponse<Unit>>
{
    public async Task<BaseResponse<Unit>> Handle(AddPatientMedicationsCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANAGE_PATIENTS_MEDICATIONS, cancellationToken))
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                StatusCode = 403,
                Message = "Forbidden",
                Errors = ["Forbidden! You do not have permission to use this command."]
            };
        }

        var validation = new AddPatientMedicationsCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Validation Failed",
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            };
        }
        
        var drugsExist = await dbContext.Drugs
            .AnyAsync(d => command.Medications.Select(m => m.DrugId).Contains(d.Id), cancellationToken);
        if (!drugsExist)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = "Not Found",
                Errors = ["One or more of the specified drugs were not found."]
            };
        }
        
        var drugIds = command.Medications.Select(m => m.DrugId).ToList();
        
        // validations and double-checking for data consistencies
        

        return new BaseResponse<Unit>
        {
            IsSuccess = true,
        };
    }
}