namespace Triumph.HealthMS.Application.Features.HealthOrganizations.CreateAnOrganization;

public record CreateAnOrganizationRequest(
    string OrganizationTitle,
    string Address,
    string MainPhone,
    string Email,
    Guid SubscriptionId,
    string FirstName,
    string LastName,
    string OtherNames,
    string PhoneNumber,
    string EmailAddress,
    string DateOfBirth,
    string? EmployedAt);

public record CreateAnOrganizationResponse(Guid TenantId, Guid SubscriptionId);

public record CreateAnOrganizationCommand(
    string OrganizationTitle,
    string Address,
    string MainPhone,
    string Email,
    Guid SubscriptionId,
    string FirstName,
    string LastName,
    string OtherNames,
    string PhoneNumber,
    string EmailAddress,
    DateOnly DateOfBirth,
    DateTime? EmployedAt) : ICommand<BaseResponse<CreateAnOrganizationResponse>>;