namespace Triumph.HealthMS.Application.Features.Employees.CreateAnEmployee;

public sealed class CreateAnEmployeeCommandHandler(
    IApplicationDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    ITenantContext tenantContext,
    ILogger<CreateAnEmployeeCommandHandler> logger,
    IPermissionsServices permissionsServices)
    : IRequestHandler<CreateAnEmployeeCommand, BaseResponse<CreateAnEmployeeResponse>>
{
    public async Task<BaseResponse<CreateAnEmployeeResponse>> Handle(CreateAnEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (!(await permissionsServices.HasPermission(PermissionValue.MANANGE_EMPLOYEES, cancellationToken)))
        {
            return new BaseResponse<CreateAnEmployeeResponse>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Unauthorized. You do not have permission to manage employees."]
            };
        }

        var validation = new CreateAnEmployeeCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
        {
            return new BaseResponse<CreateAnEmployeeResponse>
            {
                IsSuccess = false,
                Message = "Validation Failed",
                StatusCode = 400,
                Errors = validationResult.Errors.Select(x => x.ErrorMessage)
            };
        }

        HealthFacility? healthFacility = null;
        if (command.FacilityId.HasValue)
        {
            healthFacility = await dbContext
                .HealthFacilities
                .FirstOrDefaultAsync(x => x.Id == command.FacilityId.Value, cancellationToken);

            if (healthFacility is null)
            {
                return new BaseResponse<CreateAnEmployeeResponse>
                {
                    IsSuccess = false,
                    Message = "Not Found",
                    StatusCode = 404,
                    Errors = ["Health Facility Not Found."]
                };
            }
        }

        Department? department = null;
        if (command.DepartmentId.HasValue)
        {
            department = await dbContext
                .Departments
                .FirstOrDefaultAsync(x => x.Id == command.DepartmentId.Value, cancellationToken);

            if (department is null)
            {
                return new BaseResponse<CreateAnEmployeeResponse>
                {
                    IsSuccess = false,
                    Message = "Not Found",
                    StatusCode = 404,
                    Errors = ["Department Not Found."]
                };
            }
        }

        var role = await dbContext
            .Roles
            .FirstOrDefaultAsync(x => x.Id == command.RoleId, cancellationToken);

        if (role is null)
        {
            return new BaseResponse<CreateAnEmployeeResponse>
            {
                IsSuccess = false,
                Message = "Not Found",
                StatusCode = 404,
                Errors = ["Role Not Found."]
            };
        }

        // create an application user
        var newApplicationUser = new ApplicationUser
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            OtherNames = command.OtherNames ?? string.Empty,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            DateOfBirth = command.DateOfBirth,
            UserId = "temporal-user", // to be updated upon user linkage after invitation acceptance,
            Gender = Enum.Parse<Gender>(command.Gender),
            Nationality = command.Nationality
        };
        await dbContext.ApplicationUsers.AddAsync(newApplicationUser, cancellationToken);

        // create an employee associated to the user
        var newEmployee = new Employee
        {
            ApplicationUserId = newApplicationUser.Id,
            TenantId = tenantContext.TenantId,
            HealthFacility = healthFacility,
            Department = department,
            EmployedAt = command.EmployedAt ?? DateTime.UtcNow,
            UniqueIdentifier = $"EMP-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
        };

        newEmployee.Roles.Add(new EmployeeRole
        {
            EmployeeId = newEmployee.Id,
            RoleId = command.RoleId,
            ResumedRoleAt = DateTime.UtcNow
        });

        // assign permissions
        if (command.Permissions is not null && command.Permissions.Any())
        {
            var permissionCheckResult = await PermissionsExist(command
                    .Permissions
                    .Select(x => Enum.Parse<PermissionValue>(x))
                    .ToList(),
                cancellationToken);

            if (!permissionCheckResult.Item1)
            {
                return new BaseResponse<CreateAnEmployeeResponse>
                {
                    IsSuccess = false,
                    Message = "Not Found",
                    StatusCode = 404,
                    Errors = ["One or more permissions are were not found."]
                };
            }

            newEmployee.Permissions = permissionCheckResult.Item2.Select(x => new EmployeePermission
            {
                EmployeeId = newEmployee.Id,
                PermissionId = x.Id,
            }).ToList();
        }
        await dbContext.Employees.AddAsync(newEmployee, cancellationToken);

        // save changes
        await dbContext.SaveChangesAsync(cancellationToken);

        // publish for invitation link for userid linkage
        await PublishEmployeeCreatedEvent(newEmployee, cancellationToken);

        return new BaseResponse<CreateAnEmployeeResponse>
        {
            IsSuccess = true,
            Message = "Created An Employee",
            StatusCode = 201,
            Data = new CreateAnEmployeeResponse(newEmployee.Id),
        };
    }

    private async Task<(bool, List<Permission>)> PermissionsExist(List<PermissionValue> permissions, CancellationToken cancellationToken)
    {
        var query = await dbContext
            .Permissions
            .Where(x => permissions.Contains(x.Value))
            .ToListAsync(cancellationToken);

        return (query.Count == permissions.Count, query);
    }

    private async Task PublishEmployeeCreatedEvent(Employee employee, CancellationToken cancellationToken)
    {
        var employeeCreatedEvent = new EmployeeCreatedEvent
        {
            EmployeeId = employee.Id,
            CreatedBy = tenantContext.UserId,
            Message = $"Employee with ID {employee.Id} has been created.",
            ResourceType = nameof(Employee),
            ResourceTypeId = employee.Id
        };

        try
        {
            await publishEndpoint.Publish(employeeCreatedEvent, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to publish EmployeeCreatedEvent for Employee ID {EmployeeId} with Event {Event}. Error: {Error}",
                employee.Id, employeeCreatedEvent, e);
        }
    }
}