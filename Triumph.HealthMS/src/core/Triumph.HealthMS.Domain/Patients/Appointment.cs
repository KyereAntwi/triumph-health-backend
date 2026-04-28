namespace Triumph.HealthMS.Domain.Patients;

public class Appointment : TenantEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateOnly DueDate { get; set; }
    public TimeOnly StartAt { get; set; }
    public TimeOnly EndAt { get; set; }
}