namespace Triumph.HealthMS.Domain.Patients;

public class PatientAllergy : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid AllergyId { get; set; }
    public Allergy? Allergy { get; set; }

    public string ReactionDetails { get; set; } = string.Empty;
    public DateTime FirstIdentifiedAt { get; set; }
}