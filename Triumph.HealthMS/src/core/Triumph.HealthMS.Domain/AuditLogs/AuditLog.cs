namespace Triumph.HealthMS.Domain.AuditLogs;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid? FacilityId { get; set; }

    public string UserId { get; set; } = default!;

    public string Action { get; set; } = default!; // Create, Update, Delete
    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }

    public object? Before { get; set; }
    public object? After { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? TraceId { get; set; }
}