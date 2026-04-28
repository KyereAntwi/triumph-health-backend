namespace Triumph.HealthMS.Application.Features.Patients.AddConsultation;

public class AddConsultationCommandValidator : AbstractValidator<AddConsultationCommand>
{
    public AddConsultationCommandValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithMessage("DoctorId is required");
        
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("PatientId is required");
    }
}