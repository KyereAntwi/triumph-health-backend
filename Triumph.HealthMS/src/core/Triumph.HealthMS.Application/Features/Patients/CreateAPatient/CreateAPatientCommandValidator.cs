namespace Triumph.HealthMS.Application.Features.Patients.CreateAPatient;

public class CreateAPatientCommandValidator : AbstractValidator<CreateAPatientCommand>
{
    public CreateAPatientCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(15).WithMessage("First name cannot exceed 15 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(15).WithMessage("Last name cannot exceed 15 characters.");
        
        RuleFor(x => x.OtherNames)
            .MaximumLength(15).WithMessage("Other names cannot exceed 15 characters.")
            .When(x => !string.IsNullOrEmpty(x.OtherNames));
        
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .When(x => !string.IsNullOrEmpty(x.Email));
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .When(x => x.SendLinkForPatientAccountLinking);
        
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(15).WithMessage("Phone number cannot exceed 15 characters.")
            .NotEmpty().WithMessage("Phone number is required.");
        
        RuleFor(x => x.Gender)
            .MaximumLength(6).WithMessage("Gender cannot exceed 6 characters.")
            .NotEmpty().WithMessage("Gender is required.")
            .Must(value => Enum.TryParse<Gender>(value, out _))
            .WithMessage("Invalid gender.");
        
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Now))
            .WithMessage("Date of birth cannot be in the future.");
        
        RuleFor(x => x.Nationality)
            .NotEmpty().WithMessage("Nationality is required.")
            .MaximumLength(15).WithMessage("Nationality cannot exceed 15 characters.");
        
        RuleFor(x => x.PassportNumber)
            .MaximumLength(15).WithMessage("Passport number cannot exceed 15 characters.")
            .When(x => !string.IsNullOrEmpty(x.PassportNumber));
        
        RuleFor(x => x.HomeAddress)
            .MaximumLength(100).WithMessage("Home address cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.HomeAddress));
        
        RuleFor(x => x.NationalIdNumber)
            .NotEmpty().WithMessage("National ID number is required.")
            .MaximumLength(20).WithMessage("National ID number cannot exceed 20 characters.");
    }
}