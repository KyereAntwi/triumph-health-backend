namespace Triumph.HealthMS.Application.Features.Employees.LinkInvitedEmployeeToCreatedAccount;

public record LinkInvitedEmployeeToCreatedAccountRequest(
    string EmployeeId);

public record LinkInvitedEmployeeToCreatedAccountCommand(
    Guid EmployeeId) : ICommand<BaseResponse<string>>;