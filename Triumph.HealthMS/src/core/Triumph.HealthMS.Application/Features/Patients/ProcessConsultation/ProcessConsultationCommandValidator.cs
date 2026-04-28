namespace Triumph.HealthMS.Application.Features.Patients.ProcessConsultation;

public class ProcessConsultationCommandValidator : AbstractValidator<ProcessConsultationCommand>
{
    public ProcessConsultationCommandValidator()
    {
        RuleFor(x => x.ConsultationId).NotEmpty().WithMessage("ConsultationId is required.");

        RuleFor(x => x.Notes)
        .NotEmpty().WithMessage("Notes are required.")
        .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}
