namespace Triumph.HealthMS.Domain.Common;

public class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool Deleted { get; set; } =  false;
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}