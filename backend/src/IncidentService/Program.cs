using System.Text.Json;
using IncidentScope.Data.Postgres;
using IncidentScope.Observability;
using IncidentScope.Security.Tenant;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("x-tenant-id", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "x-tenant-id",
        Description = "Tenant ID header (required for all requests). Use '00000000-0000-0000-0000-000000000001' for development.",
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "x-tenant-id"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Observability
builder.Services.AddIncidentScopeObservability(
    serviceName: "incident-service",
    serviceVersion: "1.0.0",
    otlpEndpoint: builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

// Database
var connectionString = builder.Configuration.GetConnectionString("Postgres") 
    ?? "Host=localhost;Port=5432;Database=incidentscope;Username=incidentscope;Password=incidentscope-dev";
builder.Services.AddDbContext<IncidentScopeDbContext>(options =>
    options.UseNpgsql(connectionString));

// Tenant middleware is registered via UseMiddleware, no need to register here

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "IncidentScope Incident Service");
        c.EnablePersistAuthorization();
    });
}

app.UseMiddleware<TenantMiddleware>();
app.UseHttpsRedirection();

// Minimal API endpoints
app.MapPost("/incidents", async (
    HttpContext context,
    IncidentScopeDbContext db,
    CreateIncidentRequest request) =>
{
    var tenantContext = context.GetTenantContext();
    
    // Handle empty or null primary service ID
    Guid? primaryServiceId = null;
    if (!string.IsNullOrWhiteSpace(request.PrimaryServiceId) && Guid.TryParse(request.PrimaryServiceId, out var parsedServiceId))
    {
        primaryServiceId = parsedServiceId;
    }
    
    var incidentId = Guid.NewGuid();
    var tenantId = Guid.Parse(tenantContext.TenantId);
    var envId = Guid.Parse(request.EnvId);
    var severity = request.Severity;
    var status = "open";
    var title = request.Title;
    var createdAt = DateTime.UtcNow;
    var detectedAt = request.DetectedAtUnixMs > 0 
        ? DateTimeOffset.FromUnixTimeMilliseconds(request.DetectedAtUnixMs).UtcDateTime 
        : DateTime.UtcNow;
    var createdBy = request.CreatedBy ?? tenantContext.UserId;
    var labels = request.Labels ?? new Dictionary<string, string>();

    // TODO: Use proper entity framework entities
    // For now, using raw SQL for MVP
    // Use conditional SQL to handle NULL primary_service_id
    if (primaryServiceId.HasValue)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO incidents (id, tenant_id, env_id, primary_service_id, severity, status, title, 
                                  created_at, detected_at, created_by, labels)
            VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}::jsonb)",
            incidentId, tenantId, envId, primaryServiceId.Value,
            severity, status, title, createdAt, detectedAt, createdBy, JsonSerializer.Serialize(labels));
    }
    else
    {
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO incidents (id, tenant_id, env_id, primary_service_id, severity, status, title, 
                                  created_at, detected_at, created_by, labels)
            VALUES ({0}, {1}, {2}, NULL, {3}, {4}, {5}, {6}, {7}, {8}, {9}::jsonb)",
            incidentId, tenantId, envId,
            severity, status, title, createdAt, detectedAt, createdBy, JsonSerializer.Serialize(labels));
    }
    
    var incident = new
    {
        Id = incidentId,
        TenantId = tenantId,
        EnvId = envId,
        PrimaryServiceId = primaryServiceId,
        Severity = severity,
        Status = status,
        Title = title,
        CreatedAt = createdAt,
        DetectedAt = detectedAt,
        ResolvedAt = (DateTime?)null,
        CreatedBy = createdBy,
        Assignee = (string?)null,
        Labels = labels
    };

    // Create initial event
    await db.Database.ExecuteSqlRawAsync(@"
        INSERT INTO incident_events (id, tenant_id, incident_id, type, payload)
        VALUES (gen_random_uuid(), {0}, {1}, 'detection', {2}::jsonb)",
        tenantId, incidentId, JsonSerializer.Serialize(new { source = "manual" }));

    return Results.Created($"/incidents/{incidentId}", incident);
})
.WithName("CreateIncident")
.WithOpenApi();

app.MapGet("/incidents/{id}", async (
    HttpContext context,
    IncidentScopeDbContext db,
    string id) =>
{
    // Reject "new" and other non-GUID values - this endpoint is for fetching existing incidents
    if (id == "new" || !Guid.TryParse(id, out var incidentId))
    {
        return Results.BadRequest("Invalid incident ID. Expected a valid GUID.");
    }
    
    var tenantContext = context.GetTenantContext();
    var tenantId = Guid.Parse(tenantContext.TenantId);

    // TODO: Use proper entity queries with FromSqlRaw
    // For MVP, using direct SQL query with ADO.NET
    // Get a fresh connection to avoid EF transaction/prepared statement issues
    var connectionString = db.Database.GetConnectionString();
    await using var npgsqlConnection = new NpgsqlConnection(connectionString);
    await npgsqlConnection.OpenAsync();
    
    using var command = new NpgsqlCommand(@"
        SELECT id, tenant_id, env_id, primary_service_id, severity, status, title,
               created_at, detected_at, resolved_at, created_by, assignee, labels::text
        FROM incidents
        WHERE id = @incidentId AND tenant_id = @tenantId", npgsqlConnection);
    
    var parameters = new List<NpgsqlParameter>
    {
        new NpgsqlParameter("incidentId", incidentId),
        new NpgsqlParameter("tenantId", tenantId)
    };
    command.Parameters.AddRange(parameters.ToArray());
    
    using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
    if (!await reader.ReadAsync())
    {
        return Results.NotFound();
    }

    var incident = new IncidentDto
    {
        Id = reader.GetGuid(0),
        TenantId = reader.GetGuid(1),
        EnvId = reader.GetGuid(2),
        PrimaryServiceId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
        Severity = reader.GetInt32(4),
        Status = reader.GetString(5),
        Title = reader.GetString(6),
        CreatedAt = reader.GetDateTime(7),
        DetectedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
        ResolvedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
        CreatedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
        Assignee = reader.IsDBNull(11) ? null : reader.GetString(11),
        Labels = reader.IsDBNull(12) ? "{}" : reader.GetString(12)
    };

    if (incident == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(incident);
})
.WithName("GetIncident")
.WithOpenApi();

app.MapGet("/incidents", async (
    HttpContext context,
    IncidentScopeDbContext db,
    string? envId = null,
    string? status = null,
    int? severity = null) =>
{
    var tenantContext = context.GetTenantContext();
    var tenantId = Guid.Parse(tenantContext.TenantId);

    // Build query with explicit column names to ensure correct order
    var query = @"SELECT id, tenant_id, env_id, primary_service_id, severity, status, title,
                         created_at, detected_at, resolved_at, created_by, assignee, labels::text
                  FROM incidents WHERE tenant_id = @tenantId";
    var parameters = new List<NpgsqlParameter>
    {
        new NpgsqlParameter("tenantId", tenantId)
    };

    if (!string.IsNullOrEmpty(envId))
    {
        query += " AND env_id = @envId";
        parameters.Add(new NpgsqlParameter("envId", Guid.Parse(envId)));
    }

    if (!string.IsNullOrEmpty(status))
    {
        query += " AND status = @status";
        parameters.Add(new NpgsqlParameter("status", status));
    }

    if (severity.HasValue)
    {
        query += " AND severity = @severity";
        parameters.Add(new NpgsqlParameter("severity", severity.Value));
    }

    query += " ORDER BY created_at DESC LIMIT 100";

    // For MVP, using ADO.NET directly with Npgsql
    // Get a fresh connection to avoid EF transaction/prepared statement issues
    var connectionString = db.Database.GetConnectionString();
    
    await using var npgsqlConnection = new NpgsqlConnection(connectionString);
    await npgsqlConnection.OpenAsync();
    
    using var command = new NpgsqlCommand(query, npgsqlConnection);
    
    // Add all parameters at once
    command.Parameters.AddRange(parameters.ToArray());

    var incidents = new List<IncidentDto>();
    using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
    while (await reader.ReadAsync())
    {
        incidents.Add(new IncidentDto
        {
            Id = reader.GetGuid(0),
            TenantId = reader.GetGuid(1),
            EnvId = reader.GetGuid(2),
            PrimaryServiceId = reader.IsDBNull(3) ? null : reader.GetGuid(3),
            Severity = reader.GetInt32(4),
            Status = reader.GetString(5),
            Title = reader.GetString(6),
            CreatedAt = reader.GetDateTime(7),
            DetectedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
            ResolvedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            CreatedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
            Assignee = reader.IsDBNull(11) ? null : reader.GetString(11),
            Labels = reader.IsDBNull(12) ? "{}" : reader.GetString(12)
        });
    }

    return Results.Ok(incidents);
})
.WithName("ListIncidents")
.WithOpenApi();

app.MapPost("/incidents/{id}/resolve", async (
    HttpContext context,
    IncidentScopeDbContext db,
    string id,
    ResolveIncidentRequest request) =>
{
    var tenantContext = context.GetTenantContext();
    var tenantId = Guid.Parse(tenantContext.TenantId);
    var incidentId = Guid.Parse(id);

    var rowsAffected = await db.Database.ExecuteSqlRawAsync(@"
        UPDATE incidents 
        SET status = 'resolved', resolved_at = {0}, assignee = {1}
        WHERE id = {2} AND tenant_id = {3}",
        DateTime.UtcNow, request.ResolvedBy ?? tenantContext.UserId, incidentId, tenantId);

    if (rowsAffected == 0)
    {
        return Results.NotFound();
    }

    // Create resolution event
    await db.Database.ExecuteSqlRawAsync(@"
        INSERT INTO incident_events (id, tenant_id, incident_id, type, payload)
        VALUES (gen_random_uuid(), {0}, {1}, 'resolution', {2}::jsonb)",
        tenantId, incidentId, JsonSerializer.Serialize(new { resolved_by = request.ResolvedBy }));

    return Results.Ok();
})
.WithName("ResolveIncident")
.WithOpenApi();

app.Run();

// DTOs
public record CreateIncidentRequest(
    string EnvId,
    string? PrimaryServiceId,
    int Severity,
    string Title,
    long DetectedAtUnixMs,
    string? CreatedBy,
    Dictionary<string, string>? Labels);

public record ResolveIncidentRequest(string? ResolvedBy);

public class IncidentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EnvId { get; set; }
    public Guid? PrimaryServiceId { get; set; }
    public int Severity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? Assignee { get; set; }
    public string Labels { get; set; } = "{}";
}

