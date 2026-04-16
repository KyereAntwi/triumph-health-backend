namespace Triumph.HealthMS.Domain.Patients;

public class Patient : TenantEntity
{
    public string? PassportNumber { get; set; }
    public string NationalIdNumber { get; set; } = string.Empty;
    public string? HomeAddress { get; set; }
    public bool Deceased { get; set; } = false;

    public string? BloodGroup { get; set; }
    public string? Genotype { get; set; }
    
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }

    public Guid FacilityId { get; set; }
    public HealthFacility? HealthFacility { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = [];
    public ICollection<PatientAllergy> PatientAllergies { get; set; } = [];
    public ICollection<PatientChronicCondition> PatientChronicConditions { get; set; } = [];
    public ICollection<Medication> Medications { get; set; } = [];
    public ICollection<PatientProcedure> PatientProcedures { get; set; } = [];
    public ICollection<PatientLabTest> PatientLabTests { get; set; } = [];
    public ICollection<Visit> Visits { get; set; } = [];
    public ICollection<PatientFacilityOpdCaptureItem> PatientFacilityOpdCaptureItems { get; set; } = [];
}