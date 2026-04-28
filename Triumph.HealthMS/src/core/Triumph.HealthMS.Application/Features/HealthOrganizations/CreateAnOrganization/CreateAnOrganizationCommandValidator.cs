namespace Triumph.HealthMS.Application.Features.HealthOrganizations.CreateAnOrganization;

public class CreateAnOrganizationCommandValidator : AbstractValidator<CreateAnOrganizationCommand>
{
    public CreateAnOrganizationCommandValidator()
    {
        RuleFor(x => x.OrganizationTitle)
            .NotEmpty()
            .WithMessage("Organization title is required.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required.");

        RuleFor(x => x.MainPhone)
            .NotEmpty()
            .WithMessage("Main phone number is required.")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Main phone number must be in a valid format.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email must be in a valid format.");

        RuleFor(x => x.SubscriptionId)
            .NotEmpty()
            .WithMessage("Subscription ID is required.");
        
        RuleFor(x => x.DateOfBirth)
            .NotNull().WithMessage("Date of birth is required.")
            .GreaterThan(DateOnly.MinValue).WithMessage("Date of birth must be a valid date.")
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).WithMessage("Date of birth must be in the past.");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in a valid format.");
        
        RuleFor(x => x.OtherNames)
            .MaximumLength(15).WithMessage("Other names cannot exceed 15 characters.")
            .MinimumLength(3).WithMessage("Other names cannot exceed 3 characters.")
            .When(x => !string.IsNullOrEmpty(x.OtherNames));
        
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MinimumLength(3).WithMessage("First name cannot exceed 3 characters.")
            .MaximumLength(15).WithMessage("First name cannot exceed 15 characters.");
        
        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MinimumLength(3).WithMessage("Last name cannot exceed 3 characters.")
            .MaximumLength(15).WithMessage("Last name cannot exceed 15 characters.");
        
        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Email address must be in a valid format.");
    }
}