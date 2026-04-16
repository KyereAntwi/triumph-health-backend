namespace Triumph.HealthMS.Host.Middlewares;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, ITenantContext tenantContext, IApplicationDbContext dbContext)
    {
        if (httpContext.Request.Headers["X-Onboarding-Tenant"].Count > 0)
        {
            await _next(httpContext);
            return;
        }
        
        var user = httpContext.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            tenantContext.IsAuthenticated = true;
            
            tenantContext.UserId = 
                user.FindFirst("sub")?.Value ?? 
                throw new UnauthorizedAccessException("User ID missing");
            
            if (!await dbContext.ApplicationUsers.AnyAsync(u => u.UserId == tenantContext.UserId))
                throw new UnauthorizedAccessException("User not found");

            if (httpContext.Request.Headers["X-Employee-Account-Link"].Count > 0)
            {
                await _next(httpContext);
                return;
            }
            
            var tenantId = 
                user.FindFirst("tenant_id")?.Value ?? 
                httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(tenantId))
                throw new BadHttpRequestException("TenantId is required");

            tenantContext.TenantId = Guid.Parse(tenantId);
            
            var facilityId = httpContext.Request.Headers["X-Facility-Id"].FirstOrDefault();

            tenantContext.FacilityId = string.IsNullOrEmpty(facilityId)
                ? null
                : Guid.Parse(facilityId);
        }
        else
        {
            tenantContext.IsAuthenticated = false;
        }

        await _next(httpContext);
    }
}