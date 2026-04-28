namespace Triumph.HealthMS.Application.Features.Patients.CreateAPatient;

public record PatientCreatedEvent : IntegrationEvent
{
    public Guid PatientId { get; set; }
}