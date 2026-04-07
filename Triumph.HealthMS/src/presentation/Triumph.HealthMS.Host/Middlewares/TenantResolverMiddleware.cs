namespace Triumph.HealthMS.Host.Middlewares;

public class TenantResolverMiddleware
{
    private readonly RequestDelegate _next;
    
    public TenantResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, ITenantContext tenantContext)
    {
        var user =  httpContext.User;
        
        if (user.Identity?.IsAuthenticated == true)
        {
            tenantContext.IsAuthenticated = true;

            // UserId (from Keycloak)
            tenantContext.UserId = 
                user.FindFirst("sub")?.Value ?? 
                throw new UnauthorizedAccessException("User ID missing");

            // TenantId (custom claim OR header fallback)
            var tenantId = 
                user.FindFirst("tenant_id")?.Value ?? 
                httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(tenantId))
                throw new Exception("TenantId is required");

            tenantContext.TenantId = Guid.Parse(tenantId);

            // FacilityId (header for now)
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