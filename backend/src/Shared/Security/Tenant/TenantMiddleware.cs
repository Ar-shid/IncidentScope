using IncidentScope.Security.Tenant;

namespace IncidentScope.Security.Tenant;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Request.Headers["x-tenant-id"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Missing x-tenant-id header");
            return;
        }

        var tenantContext = new TenantContext
        {
            TenantId = tenantId,
            UserId = context.User.Identity?.Name,
            Roles = context.User.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value)
                .ToList()
        };

        context.Items["TenantContext"] = tenantContext;
        await _next(context);
    }
}

