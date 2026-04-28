namespace Triumph.HealthMS.Domain.Common;

public class Subscription : BaseEntity
{
    public string Title { get; set; } =  string.Empty;
    public string Description { get; set; } =  string.Empty;
    public int ActivePeriodInMonths { get; set; }
}