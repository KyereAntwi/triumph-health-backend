namespace Triumph.HealthMS.Application.Features.Patients.CapturePatientMeasurableItems;

public class CapturePatientMeasurableItemsCommandValidator : AbstractValidator<CapturePatientMeasurableItemsCommand>
{
    public CapturePatientMeasurableItemsCommandValidator()
    {
        RuleFor(c => c.PatientId)
            .NotEmpty()
            .WithMessage("PatientId cannot be empty");
        
        RuleFor(c => c.VisitId)
            .NotEmpty()
            .WithMessage("VisitId cannot be empty");
        
        RuleFor(c => c.OpdItems)
            .NotEmpty()
            .WithMessage("OpdItems cannot be empty")
            .Must(list => list.All(
                item => item.FacilityOpdCaptureItemId != Guid.Empty && !string.IsNullOrEmpty(item.ValueCaptured)))
            .WithMessage("OpdItems cannot be empty");;
    }
}