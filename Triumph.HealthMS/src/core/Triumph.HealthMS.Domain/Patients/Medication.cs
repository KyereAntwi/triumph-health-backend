namespace Triumph.HealthMS.Domain.Patients;

public class Medication : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid DrugId { get; set; }
    public Drug? Drug { get; set; }

    public string? AdditionalPrescription { get; set; }
    public bool IsCurrentlyBeenTaken { get; set; } = false;
    public DateTime PrescribedAt { get; set; }

    public Guid? AssociatedVisitId { get; set; }

    public decimal AmountPaid { get; set; }
    public int Quantity { get; set; } = 1;
}