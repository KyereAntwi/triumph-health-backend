namespace Triumph.HealthMS.Domain.Tenants;

public class FacilityProcedure : TenantEntity
{
    public Guid ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }

    public Guid FacilityId { get; set; }
    [ForeignKey("FacilityId")]
    public HealthFacility? HealthFacility { get; set; }

    public string? AdditionalDetails { get; set; }
}