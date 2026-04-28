namespace Triumph.HealthMS.Queries.Queries;

public class QueryBase
{
    private readonly EmployeesQuery _employeesQuery;

    public QueryBase(EmployeesQuery employeesQuery)
    {
        _employeesQuery = employeesQuery;
    }
    public async Task<List<EmployeeDto>> Employees()
    {
        var list = await _employeesQuery.ExecuteAsync(null, 1, 10, CancellationToken.None);
        return list.Data.ToList();
    }
}