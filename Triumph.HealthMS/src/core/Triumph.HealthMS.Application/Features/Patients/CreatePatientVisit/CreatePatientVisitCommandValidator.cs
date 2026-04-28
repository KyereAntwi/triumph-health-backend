namespace Triumph.HealthMS.Application.Features.Patients.CreatePatientVisit;

public class CreatePatientVisitCommandValidator : AbstractValidator<CreatePatientVisitCommand>
{
    public CreatePatientVisitCommandValidator()
    {
        RuleFor(c => c.PatientId)
            .NotEmpty()
            .WithMessage("PatientId cannot be empty");
        
        RuleFor(c => c.VisitReason)
            .NotEmpty()
            .WithMessage("VisitReason cannot be empty");
    }
}