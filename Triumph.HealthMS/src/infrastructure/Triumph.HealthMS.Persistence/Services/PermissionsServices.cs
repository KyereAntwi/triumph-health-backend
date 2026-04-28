namespace Triumph.HealthMS.Persistence.Services;

public class PermissionsServices : IPermissionsServices
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public PermissionsServices(IApplicationDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }
    
    public async Task<bool> HasPermission(PermissionValue permission, CancellationToken cancellationToken = default)
    {
        var query = await _dbContext.Employees
            .Select(e => new
            {
                e.ApplicationUser!.UserId,
                Permissions = e.Permissions.Select(p => p.Permission!.Value)
            })
            .Where(e => 
                e.UserId == _tenantContext.UserId &&
                e.Permissions.Contains(permission))
            .ToArrayAsync(cancellationToken);
        
        return query.Length > 0;
    }
}