namespace Triumph.HealthMS.Domain.Patients;

public class Consultation : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid DoctorId { get; set; }
    public Employee? Doctor { get; set; }

    public Guid? AssociatedVisit { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string? Notes { get; set; }
    public decimal AmountPaid { get; set; }
    public int Quantity { get; set; } = 1;
}