namespace Triumph.HealthMS.Application.Features.Employees.UpdateRole;

public sealed class UpdateRoleCommandHandler(
    IApplicationDbContext dbContext,
    IPermissionsServices permissionsServices)
    : IRequestHandler<UpdateRoleCommand, BaseResponse<string>>
{
    public async Task<BaseResponse<string>> Handle(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANANGE_EMPLOYEES, cancellationToken))
        {
            return new BaseResponse<string>
            {
                IsSuccess = false,
                Message = "Forbidden",
                StatusCode = 403,
                Errors = ["Forbidden. You are not allowed to perform this action."]
            };
        }
        
        var existedEmployee = await dbContext
            .Employees
            .Select(e => new 
            {
                e.Id,
                role = e.Roles.OrderByDescending(r => r.CreatedAt).FirstOrDefault()
            })
            .FirstOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);

        if (existedEmployee is null)
        {
            return new BaseResponse<string>
            {
                IsSuccess = false,
                Message = "Not found",
                StatusCode = 404,
                Errors = [$"Employee with Id {command.EmployeeId} was not found."]
            };
        }
        
        var existedRole = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);

        if (existedRole is null)
        {
            return new BaseResponse<string>
            {
                IsSuccess = false,
                Message = "Not found",
                StatusCode = 404,
                Errors = [$"Role with Id {command.RoleId} was not found."]
            };
        }
        
        existedEmployee.role.EndedRoleAt = DateTime.UtcNow;
        
        dbContext.EmployeeRoles.Update(existedEmployee.role);
        await dbContext.EmployeeRoles.AddAsync(new EmployeeRole
        {
            EmployeeId = command.EmployeeId,
            RoleId = command.RoleId,
            ResumedRoleAt = command.ResumedDate
        }, cancellationToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<string>
        {
            IsSuccess = true,
            Message = "Updated",
            StatusCode = 200
        };
    }
}