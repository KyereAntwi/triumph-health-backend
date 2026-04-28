namespace Triumph.HealthMS.Domain.Patients;

public class PatientChronicCondition : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid ConditionId { get; set; }
    [ForeignKey("ConditionId")]
    public ChronicCondition? Condition { get; set; }

    public DateOnly FirstIdentifiedAt { get; set; }
}