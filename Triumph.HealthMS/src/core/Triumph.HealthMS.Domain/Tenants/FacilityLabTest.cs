namespace Triumph.HealthMS.Domain.Tenants;

public class FacilityLabTest : TenantEntity
{
    public Guid FacilityId { get; set; }
    [ForeignKey("FacilityId")]
    public HealthFacility? HealthFacility { get; set; }

    public Guid LabTestId { get; set; }
    public LabTest? LabTest { get; set; }

    public string? AdditionalDescription { get; set; }
}