namespace Triumph.HealthMS.Domain.Common;

public class ApplicationUser : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } =  string.Empty;
    public string LastName { get; set; } =  string.Empty;
    public string? OtherNames { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public DateOnly DateOfBirth { get; set; }

    public ICollection<Patient> Patients { get; set; } = [];
}