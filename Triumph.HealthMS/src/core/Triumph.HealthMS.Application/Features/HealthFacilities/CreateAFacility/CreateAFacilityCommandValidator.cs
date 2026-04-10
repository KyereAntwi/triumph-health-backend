namespace Triumph.HealthMS.Application.Features.HealthFacilities.CreateAFacility;

public class CreateAFacilityCommandValidator : AbstractValidator<CreateAFacilityCommand>
{
    public CreateAFacilityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Facility name is required.")
            .MaximumLength(100)
            .WithMessage("Facility name cannot exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Facility address is required.")
            .MaximumLength(100)
            .WithMessage("Facility address cannot exceed 100 characters.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Facility location is required.")
            .MaximumLength(100)
            .WithMessage("Facility location cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Facility email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.");

        RuleFor(x => x.MainPhone)
            .NotEmpty()
            .WithMessage("Facility main phone number is required.")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Invalid phone number format.");
    }
}