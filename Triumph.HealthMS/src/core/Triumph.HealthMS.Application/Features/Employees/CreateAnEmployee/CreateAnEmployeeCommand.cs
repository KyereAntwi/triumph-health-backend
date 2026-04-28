namespace Triumph.HealthMS.Application.Features.Employees.CreateAnEmployee;

public record CreateAnEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string? OtherNames,
    string DateOfBirth,
    string? FacilityId,
    string? DepartmentId,
    string? RoleId,
    string? EmployedAt,
    string Gender,
    string Nationality,
    IEnumerable<string>? Permissions);

public record CreateAnEmployeeResponse(
    Guid Id);

public record CreateAnEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string? OtherNames,
    DateOnly DateOfBirth,
    Guid? FacilityId,
    Guid? DepartmentId,
    Guid RoleId,
    DateTime? EmployedAt,
    string Gender,
    string Nationality,
    IEnumerable<string>? Permissions) : ICommand<BaseResponse<CreateAnEmployeeResponse>>;