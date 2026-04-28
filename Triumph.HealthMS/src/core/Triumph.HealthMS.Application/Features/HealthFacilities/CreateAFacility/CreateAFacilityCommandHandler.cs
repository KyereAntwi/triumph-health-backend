namespace Triumph.HealthMS.Application.Features.HealthFacilities.CreateAFacility;

public sealed class CreateAFacilityCommandHandler(
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext,
    ILogger<CreateAFacilityCommandHandler> logger,
    ITenantContext tenantContext)
    : IRequestHandler<CreateAFacilityCommand, BaseResponse<CreateFacilityResponse>>
{
    public async Task<BaseResponse<CreateFacilityResponse>> Handle(CreateAFacilityCommand request, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANAGE_HEALTH_FACILITIES, cancellationToken))
        {
            return new BaseResponse<CreateFacilityResponse>
            {
                IsSuccess = false,
                Message = "Unauthorized",
                Errors = ["Unauthorized: You do not have permission to manage health facilities."],
                StatusCode = 403
            };
        }
        
        var validation = new CreateAFacilityCommandValidator();
        var validationResult = await validation.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<CreateFacilityResponse>
            {
                IsSuccess = false,
                Message = "Validation Failed",
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                StatusCode = 400
            };
        }
        
        var facilityAlreadyExists = await dbContext.HealthFacilities
            .AnyAsync(f => f.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (facilityAlreadyExists)
        {
            return new BaseResponse<CreateFacilityResponse>
            {
                IsSuccess = false,
                Message = "Conflict",
                Errors = ["A health facility with the same name already exists for this organization."],
                StatusCode = 409
            };
        }
        
        var newFacility = await dbContext.HealthFacilities.AddAsync(new HealthFacility
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            Location = request.Location,
            Email = request.Email,
            MainPhone = request.MainPhone,
            TenantId = tenantContext.TenantId
        }, cancellationToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<CreateFacilityResponse>
        {
            IsSuccess = true,
            Message = "Success",
            StatusCode = 201,
            Data = new CreateFacilityResponse(newFacility.Entity.Id)
        };
    }
}