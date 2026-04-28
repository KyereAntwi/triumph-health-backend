namespace Triumph.HealthMS.Application.Features.Employees.CreateAnEmployee;

public record EmployeeCreatedEvent : IntegrationEvent
{
    public Guid EmployeeId { get; set; }
}