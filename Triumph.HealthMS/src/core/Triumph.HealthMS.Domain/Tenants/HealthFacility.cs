namespace Triumph.HealthMS.Domain.Tenants;

public class HealthFacility : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MainPhone { get; set; } = string.Empty;
    
    [ForeignKey("TenantId")]
    public HealthOrganization? HealthOrganization { get; set; }
    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Patient> Patients { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<FacilityOpdCaptureItem> FacilityOpdCaptureItems { get; set; } = [];
    public ICollection<FacilityProcedure> FacilityProcedures { get; set; } = [];
    public ICollection<FacilityLabTest> FacilityLabTests { get; set; } = [];
}