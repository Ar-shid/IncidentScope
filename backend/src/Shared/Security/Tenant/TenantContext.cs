using Microsoft.AspNetCore.Http;

namespace IncidentScope.Security.Tenant;

public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public List<string> Roles { get; set; } = new();
}

public static class TenantContextExtensions
{
    public static TenantContext GetTenantContext(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("TenantContext", out var context) && context is TenantContext tenantContext)
        {
            return tenantContext;
        }

        // Fallback: try to extract from header
        var tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Tenant context not found");
        }

        return new TenantContext
        {
            TenantId = tenantId,
            UserId = httpContext.User.Identity?.Name,
            Roles = httpContext.User.Claims
                .Where(c => c.Type == "role")
                .Select(c => c.Value)
                .ToList()
        };
    }
}

