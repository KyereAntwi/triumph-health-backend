namespace Triumph.HealthMS.Application.Features.Patients.StartAConsultation;

public class StartAConsultationCommandValidator : AbstractValidator<StartAConsultationCommand>
{
    public StartAConsultationCommandValidator()
    {
        RuleFor(x => x.ConsultationId).NotEmpty().WithMessage("ConsultationId is required.");
    }
}
