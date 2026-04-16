namespace Triumph.HealthMS.Domain.Patients;

public class EmergencyContact : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}