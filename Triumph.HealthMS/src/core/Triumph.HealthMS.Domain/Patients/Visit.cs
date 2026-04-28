namespace Triumph.HealthMS.Domain.Patients;

public class Visit : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string VisitReasons { get; set; } = string.Empty;
    public DateTime VisitTimeStamp { get; set; }
}