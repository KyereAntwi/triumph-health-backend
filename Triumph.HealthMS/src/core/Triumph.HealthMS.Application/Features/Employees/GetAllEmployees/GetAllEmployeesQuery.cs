namespace Triumph.HealthMS.Application.Features.Employees.GetAllEmployees;

public record EmployeeDto(
    string Id,
    string FirstName,
    string LastName,
    string OtherNames,
    string Email,
    string PhoneNumber,
    string RoleName
);

public record GetAllEmployeesQuery(
    string? SearchKeyword,
    int PageNumber,
    int PageSize,
    CancellationToken CancellationToken
) : IQuery<BaseResponse<IEnumerable<EmployeeDto>>>;