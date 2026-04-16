namespace Triumph.HealthMS.Domain.Patients;

public class PatientProcedure : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    
    public Guid FacilityProcedureId { get; set; }
    public FacilityProcedure? FacilityProcedure { get; set; }

    public Guid? PrescribedById { get; set; }
    public Guid? AssociatedVisitId { get; set; }
    
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Guid? SupervisedById { get; set; }
    public string? Notes { get; set; }

    public decimal AmountPaid { get; set; }
    public int Quantity { get; set; } = 1;
}