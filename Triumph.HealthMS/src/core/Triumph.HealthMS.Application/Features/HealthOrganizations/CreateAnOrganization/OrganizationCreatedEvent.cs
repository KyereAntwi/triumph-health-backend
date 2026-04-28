namespace Triumph.HealthMS.Application.Features.HealthOrganizations.CreateAnOrganization;

public record OrganizationCreatedEvent : IntegrationEvent
{
    public string OrganizationEmail { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string SubscriptionName { get; set; } = string.Empty;
}