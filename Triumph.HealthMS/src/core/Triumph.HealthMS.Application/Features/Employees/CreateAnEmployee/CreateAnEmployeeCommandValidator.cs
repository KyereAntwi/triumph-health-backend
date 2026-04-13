namespace Triumph.HealthMS.Application.Features.Employees.CreateAnEmployee;

public class CreateAnEmployeeCommandValidator : AbstractValidator<CreateAnEmployeeCommand>
{
    public CreateAnEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters.")
            .MinimumLength(2)
            .WithMessage("First name cannot exceed 2 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters.")
            .MinimumLength(2)
            .WithMessage("Last name cannot exceed 2 characters.");
        
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Email is invalid.");
        
        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .MaximumLength(15)
            .WithMessage("Phone number cannot exceed 15 characters.");
        
        RuleFor(x => x.OtherNames)
            .MaximumLength(50)
            .WithMessage("Other names cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.OtherNames));
        
        RuleFor(x => x.DateOfBirth)
            .NotEqual(DateOnly.MinValue)
            .WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth is invalid.");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("RoleId is required.");

        RuleFor(x => x.EmployedAt)
            .GreaterThan(DateTime.MinValue)
            .WithMessage("EmployedAt is invalid.")
            .When(x => x.EmployedAt.HasValue);
        
        RuleFor(x => x.FacilityId)
            .NotEmpty()
            .WithMessage("FacilityId is required.")
            .When(x => x.FacilityId.HasValue);
        
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage("DepartmentId is required.")
            .When(x => x.DepartmentId.HasValue);

        RuleFor(x => x.Permissions)
                .ForEach(permission =>
                {
                    permission
                        .Must(value => Enum.TryParse<PermissionValue>(value, out _))
                        .WithMessage("Permission '{PropertyValue}' is invalid.");
                })
                .When(x => x.Permissions != null && x.Permissions.Any());
    }
}