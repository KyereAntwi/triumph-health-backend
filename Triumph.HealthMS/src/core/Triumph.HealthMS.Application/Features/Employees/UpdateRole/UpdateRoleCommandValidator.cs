namespace Triumph.HealthMS.Application.Features.Employees.UpdateRole;

public class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("{PropertyName} is required.");
        
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("{PropertyName} is required.");
        
        RuleFor(x => x.ResumedDate)
            .NotEqual(DateTime.MinValue)
            .WithMessage("{PropertyName} is invalid.");
    }
}