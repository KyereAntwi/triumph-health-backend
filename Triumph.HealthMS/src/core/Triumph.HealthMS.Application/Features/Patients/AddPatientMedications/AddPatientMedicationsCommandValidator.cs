namespace Triumph.HealthMS.Application.Features.Patients.AddPatientMedications;

public class AddPatientMedicationsCommandValidator : AbstractValidator<AddPatientMedicationsCommand>
{
    public AddPatientMedicationsCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("PatientId cannot be empty");
        
        RuleFor(x => x.Medications)
            .NotEmpty()
            .WithMessage("Medications cannot be empty");
        
        RuleForEach(x => x.Medications)
            .SetValidator(new MedicationCommandDtoValidator());
    }
}

public class MedicationCommandDtoValidator : AbstractValidator<MedicationCommandDto>
{
    public MedicationCommandDtoValidator()
    {
        RuleFor(x => x.DrugId)
            .NotEmpty()
            .WithMessage("DrugId cannot be empty");
        
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
        
        RuleFor(x => x.AdditionalPrescription)
            .MaximumLength(500)
            .WithMessage("AdditionalPrescription cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.AdditionalPrescription));
    }
}