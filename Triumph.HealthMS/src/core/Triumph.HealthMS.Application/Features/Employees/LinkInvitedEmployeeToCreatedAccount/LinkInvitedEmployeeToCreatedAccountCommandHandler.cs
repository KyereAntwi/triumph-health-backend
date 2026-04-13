namespace Triumph.HealthMS.Application.Features.Employees.LinkInvitedEmployeeToCreatedAccount;

public sealed class LinkInvitedEmployeeToCreatedAccountCommandHandler(IApplicationDbContext dbContext, ITenantContext tenantContext)
: IRequestHandler<LinkInvitedEmployeeToCreatedAccountCommand, BaseResponse<string>>
{
    public async Task<BaseResponse<string>> Handle(LinkInvitedEmployeeToCreatedAccountCommand command, CancellationToken cancellationToken)
    {
        var validation = new LinkInvitedEmployeeToCreatedAccountCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<string>
            {
                Message = "Validation Failed",
                IsSuccess = false,
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray(),
                StatusCode = 400
            };
        }
        
        var existingEmployee = await dbContext
            .Employees
            .Select(e => new { e.Id, e.ApplicationUser })
            .FirstOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);

        if (existingEmployee is null)
        {
            return new BaseResponse<string>
            {
                Message = "Not Found",
                IsSuccess = false,
                Errors = ["Employee not found"],
                StatusCode = 404
            };
        }

        if (existingEmployee.ApplicationUser.UserId.Equals(tenantContext.UserId))
        {
            return new BaseResponse<string>
            {
                Message = "Already Linked",
                IsSuccess = false,
                Errors = ["Employee is already linked to the current user"],
                StatusCode = 400
            };
        }
        
        existingEmployee.ApplicationUser.UserId = tenantContext.UserId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<string>
        {
            Message = "Success",
            IsSuccess = true,
            Data = existingEmployee.Id.ToString(),
            StatusCode = 200
        };
    }
}