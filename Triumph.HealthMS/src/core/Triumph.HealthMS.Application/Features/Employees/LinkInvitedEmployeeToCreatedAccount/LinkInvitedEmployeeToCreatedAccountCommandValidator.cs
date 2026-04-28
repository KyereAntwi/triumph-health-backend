namespace Triumph.HealthMS.Application.Features.Employees.LinkInvitedEmployeeToCreatedAccount;

public class LinkInvitedEmployeeToCreatedAccountCommandValidator : AbstractValidator<LinkInvitedEmployeeToCreatedAccountCommand>
{
    public LinkInvitedEmployeeToCreatedAccountCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee Id cannot be empty");
    }
}