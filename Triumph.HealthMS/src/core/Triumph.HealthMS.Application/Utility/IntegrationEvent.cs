namespace Triumph.HealthMS.Application.Utility;

public record IntegrationEvent
{
    public Guid Id => Guid.NewGuid();
    public DateTime OccuredOn => DateTime.UtcNow;
    public string EventType => GetType().AssemblyQualifiedName!;
    public string CreatedBy { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceTypeId { get; set; }
}