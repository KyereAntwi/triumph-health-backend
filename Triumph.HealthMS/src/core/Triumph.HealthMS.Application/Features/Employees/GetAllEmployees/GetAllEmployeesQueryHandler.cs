namespace Triumph.HealthMS.Application.Features.Employees.GetAllEmployees;

public sealed class GetAllEmployeesQueryHandler(
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext
)
: IRequestHandler<GetAllEmployeesQuery, BaseResponse<IEnumerable<EmployeeDto>>>
{
    public Task<BaseResponse<IEnumerable<EmployeeDto>>> Handle(GetAllEmployeesQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
