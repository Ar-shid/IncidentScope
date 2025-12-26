using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

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
        
        // In development, use default tenant if header is missing
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            var env = context.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var isDevelopment = env?.EnvironmentName == Environments.Development || 
                              env?.EnvironmentName == null; // Default to development if not set
            
            if (isDevelopment)
            {
                // Use default tenant for development
                tenantId = "00000000-0000-0000-0000-000000000001";
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing x-tenant-id header");
                return;
            }
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

