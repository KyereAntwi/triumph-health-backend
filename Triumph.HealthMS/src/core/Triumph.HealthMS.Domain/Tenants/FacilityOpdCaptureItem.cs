namespace Triumph.HealthMS.Domain.Tenants;

public class FacilityOpdCaptureItem : TenantEntity
{
    public Guid FacilityId { get; set; }
    [ForeignKey("FacilityId")]
    public HealthFacility? HealthFacility { get; set; }

    public Guid OpdCaptureItemId { get; set; }
    [ForeignKey("OpdCaptureItemId")]
    public OpdCaptureItem? OpdCaptureItem { get; set; }

    public string? Code { get; set; }
    public string? FacilityNotesOnItem { get; set; }
}