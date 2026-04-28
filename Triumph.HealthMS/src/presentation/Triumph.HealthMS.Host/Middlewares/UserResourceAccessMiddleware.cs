namespace Triumph.HealthMS.Host.Middlewares;

public class UserResourceAccessMiddleware
{
    private readonly RequestDelegate _next;

    public UserResourceAccessMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, ITenantContext tenantContext, IApplicationDbContext dbContext)
    {
        if (
            httpContext.Request.Path.StartsWithSegments("/graphql") ||
            httpContext.Request.Path.StartsWithSegments("/favicon.svg") ||
            httpContext.Request.Path.StartsWithSegments("/scalar") ||
            httpContext.Request.Path.StartsWithSegments("/openapi") ||
            httpContext.Request.Headers["X-Onboarding-Tenant"].Count > 0)
        {
            await _next(httpContext);
            return;
        }

        var isEmployee = await dbContext.Employees
            .AnyAsync(e => e.ApplicationUser!.UserId == tenantContext.UserId);
        
        if (tenantContext.FacilityId != Guid.Empty && tenantContext.FacilityId != null)
        {
            var isFacilityEmployee = isEmployee && await dbContext.Employees
                .AnyAsync(e => e.ApplicationUser!.UserId == tenantContext.UserId 
                               && e.HealthFacility!.Id == tenantContext.FacilityId);
            
            if (isFacilityEmployee)
            {
                await _next(httpContext);
                return;
            }
            
            var isPatient = await dbContext.Patients
                .AnyAsync(p => p.ApplicationUser!.UserId == tenantContext.UserId);
            
            if (!isPatient)
                throw new UnauthorizedAccessException("You are not authorized to access this resource.");
        }
        else
        {
            if (!isEmployee)
                throw new UnauthorizedAccessException("You are not authorized to access this resource.");
        }

        await _next(httpContext);
    }
}