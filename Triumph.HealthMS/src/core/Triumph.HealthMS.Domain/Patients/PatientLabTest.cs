namespace Triumph.HealthMS.Domain.Patients;

public class PatientLabTest : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid FacilityLabTestId { get; set; }
    public FacilityLabTest? FacilityLabTest { get; set; }

    public Guid? AssociatedVisit { get; set; }
    public Guid? SupervisedById { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }

    public string? FindingSummary { get; set; }
    public string? Details { get; set; }

    public decimal AmountPaid { get; set; }
    public int Quantity { get; set; } = 1;
}