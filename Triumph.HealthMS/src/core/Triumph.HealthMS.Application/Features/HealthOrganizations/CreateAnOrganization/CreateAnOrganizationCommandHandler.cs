namespace Triumph.HealthMS.Application.Features.HealthOrganizations.CreateAnOrganization;

public sealed class CreateAnOrganizationCommandHandler(
    ITenantContext tenantContext,
    IPublishEndpoint publishEndpoint,
    ILogger<CreateAnOrganizationCommandHandler> logger,
    IApplicationDbContext dbContext)
    : IRequestHandler<CreateAnOrganizationCommand, BaseResponse<CreateAnOrganizationResponse>>
{
    public async Task<BaseResponse<CreateAnOrganizationResponse>> Handle(CreateAnOrganizationCommand command, CancellationToken cancellationToken)
    {
        var operationIsFirstAttemptForUser = !await dbContext.HealthOrganizations
                .Where(o => o.CreatedBy == tenantContext.UserId)
                .AnyAsync(cancellationToken);

            if (!operationIsFirstAttemptForUser)
                return new BaseResponse<CreateAnOrganizationResponse>
                {
                    IsSuccess = false,
                    Message = "User has already created an organization",
                    StatusCode = 400,
                    Errors = ["User has already created an organization"]
                };

            var validation = new CreateAnOrganizationCommandValidator();
            var validationResult = await validation.ValidateAsync(command,  cancellationToken);
            if (!validationResult.IsValid)
            {
                return new BaseResponse<CreateAnOrganizationResponse>
                {
                    IsSuccess = false,
                    Message = "Validation failed",
                    StatusCode = 400,
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage)
                };
            }
            
            var existingSubscription = await dbContext.Subscriptions.FindAsync([command.SubscriptionId], cancellationToken);
            if (existingSubscription is null)
                return new BaseResponse<CreateAnOrganizationResponse>
                {
                    IsSuccess = false,
                    Message = "Subscription with id {SubscriptionId} does not exist",
                    StatusCode = 404,
                    Errors = ["Subscription with id {SubscriptionId} does not exist"]
                };
            
            // This operation would
            // Create an application user
            // Create an organization as a tenant
            // Add assign the user as the first employee and assigned all organizational managers permissions
            // These would be in a transaction making user when one fails all fails
            
            var newApplicationUser = await dbContext.ApplicationUsers.AddAsync(new ApplicationUser
            {
                UserId = tenantContext.UserId,
                FirstName = command.FirstName,
                LastName = command.LastName,
                OtherNames = command.OtherNames,
                PhoneNumber = command.PhoneNumber,
                Email = command.EmailAddress,
                DateOfBirth = command.DateOfBirth
            }, cancellationToken);
            
            var newTenant = await dbContext.HealthOrganizations.AddAsync(new HealthOrganization
            {
                OrganizationTitle = command.OrganizationTitle,
                Address = command.Address,
                MainPhone = command.MainPhone,
                Email = command.Email
            }, cancellationToken);
            
            var newEmployee = await dbContext.Employees.AddAsync(new Employee
            {
                ApplicationUserId = newApplicationUser.Entity.Id,
                EmployedAt = command.EmployedAt ?? DateTime.UtcNow,
                TenantId = newTenant.Entity.Id,
            }, cancellationToken);

            List<PermissionValue> permissions =
                [
                    PermissionValue.MANAGE_HEALTH_FACILITIES, 
                    PermissionValue.VIEW_PATIENTS_PROFILE,
                    PermissionValue.UPDATE_TENANT_INFO
                ];

            var existingPermissions = await dbContext.Permissions.Where(p => permissions.Contains(p.Value))
                .ToArrayAsync(cancellationToken);
            
            foreach (var permission in existingPermissions)
                newEmployee.Entity.Permissions.Add(new EmployeePermission
                {
                    PermissionId = permission.Id
                });
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            await PublishOrganizationCreatedEvent(command, newTenant, existingSubscription, cancellationToken);

            return new BaseResponse<CreateAnOrganizationResponse>
            {
                IsSuccess = true,
                Message = "Successfully created an organization",
                StatusCode = 201,
                Data = new CreateAnOrganizationResponse(newTenant.Entity.Id, command.SubscriptionId)
            };
    }

    private async Task PublishOrganizationCreatedEvent(CreateAnOrganizationCommand command, 
        EntityEntry<HealthOrganization> newTenant, 
        Subscription existingSubscription,
        CancellationToken cancellationToken)
    {
        // publish success organization creation for welcome emailing
        var @event = new OrganizationCreatedEvent
        {
            CreatedBy = tenantContext.UserId,
            Message = $"Organization with TenantId {newTenant.Entity.Id} created successfully",
            ResourceType = nameof(HealthOrganization),
            ResourceTypeId = newTenant.Entity.Id,
            OrganizationEmail = command.Email,
            OrganizationName = command.OrganizationTitle,
            SubscriptionName = existingSubscription.Title
        };
        
        try
        {
            await publishEndpoint.Publish(@event, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to publish OrganizationCreatedEvent for TenantId {TenantId}, with payload {Event}", newTenant.Entity.Id, @event.ToString());
        }
    }
}