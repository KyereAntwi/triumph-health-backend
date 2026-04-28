namespace Triumph.HealthMS.Application.Features.Patients.LinkPatientToCreatedAccount;

public class LinkPatientToCreatedAccountCommandValidator : AbstractValidator<LinkPatientToCreatedAccountCommand>
{
    public LinkPatientToCreatedAccountCommandValidator()
    {
        RuleFor(c => c.PatientId)
            .NotEmpty()
            .WithMessage("PatientId cannot be empty");
    }
}