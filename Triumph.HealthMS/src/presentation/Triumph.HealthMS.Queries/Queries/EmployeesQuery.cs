

namespace Triumph.HealthMS.Queries.Queries;

public class EmployeesQuery(ISender sender)
{
    public async Task<BaseResponse<IEnumerable<EmployeeDto>>> ExecuteAsync(string? searchKeyword, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllEmployeesQuery(searchKeyword, pageNumber, pageSize, cancellationToken));
        return result;
    }
}
