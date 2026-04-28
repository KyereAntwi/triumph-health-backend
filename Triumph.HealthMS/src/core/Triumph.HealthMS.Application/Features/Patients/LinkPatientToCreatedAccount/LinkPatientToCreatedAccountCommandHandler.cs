namespace Triumph.HealthMS.Application.Features.Patients.LinkPatientToCreatedAccount;

public sealed class LinkPatientToCreatedAccountCommandHandler(
    ITenantContext tenantContext,
    IApplicationDbContext dbContext)
    : IRequestHandler<LinkPatientToCreatedAccountCommand, BaseResponse<Unit>>
{
    public async Task<BaseResponse<Unit>> Handle(LinkPatientToCreatedAccountCommand command, CancellationToken cancellationToken)
    {
        var validation = new LinkPatientToCreatedAccountCommandValidator();
        var validationResult = await validation.ValidateAsync(command,  cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                StatusCode = 400,
                Message = "Validation failed",
                Errors = validationResult.Errors.Select(x => x.ErrorMessage)
            };
        }
        
        var existingPatient = await dbContext
            .Patients
            .Include(p => p.ApplicationUser)
            .AsSplitQuery()
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);

        if (existingPatient is null)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                StatusCode = 404,
                Message = "Not found",
                Errors = ["Patient not found"]
            };
        }
        
        existingPatient.ApplicationUser?.UserId = tenantContext.UserId;
        dbContext.ApplicationUsers.Update(existingPatient.ApplicationUser!);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        return new BaseResponse<Unit>
        {
            IsSuccess = true,
            StatusCode = 200,
            Message = "Patient linked to account successfully",
            Data = Unit.Value
        };
    }
}