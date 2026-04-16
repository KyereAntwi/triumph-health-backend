namespace Triumph.HealthMS.Domain.Patients;

public class PatientFacilityOpdCaptureItem : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid FacilityOpdCaptureItemId { get; set; }
    public FacilityOpdCaptureItem? FacilityOpdCaptureItem { get; set; }
    public Guid AssociatedVisit { get; set; }
    
    public string ValueCaptured { get; set; } = string.Empty;
    public string? Notes { get; set; }
}