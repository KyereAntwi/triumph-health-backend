namespace Triumph.HealthMS.Application.Features.Employees.UpdateRole;

public record UpdateRoleRequest(
    string EmployeeId, 
    string RoleId,
    string ResumedDate);

public record UpdateRoleCommand(
    Guid EmployeeId,
    Guid RoleId,
    DateTime ResumedDate) : ICommand<BaseResponse<string>>;