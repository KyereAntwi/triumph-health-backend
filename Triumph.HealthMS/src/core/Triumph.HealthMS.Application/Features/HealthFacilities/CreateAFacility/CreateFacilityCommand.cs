namespace Triumph.HealthMS.Application.Features.HealthFacilities.CreateAFacility;

public record CreateFacilityRequest(
    string Name,
    string Address,
    string Location,
    string Email,
    string MainPhone);
    
public record CreateFacilityResponse(Guid Id);

public record CreateAFacilityCommand(
    string Name,
    string Address,
    string Location,
    string Email,
    string MainPhone) : ICommand<BaseResponse<CreateFacilityResponse>>;