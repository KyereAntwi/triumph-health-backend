namespace Triumph.HealthMS.Domain.Patients;

public class Patient : TenantEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public Guid FacilityId { get; set; }
    public HealthFacility? HealthFacility { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
}