using IncidentScope.Observability;
using IncidentScope.Security.Tenant;
using Microsoft.EntityFrameworkCore;
using IncidentScope.Data.Postgres;
using System.Text.Json;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Tenant middleware
builder.Services.AddScoped<TenantMiddleware>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
    
    var incident = new
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.Parse(tenantContext.TenantId),
        EnvId = Guid.Parse(request.EnvId),
        PrimaryServiceId = request.PrimaryServiceId != null ? Guid.Parse(request.PrimaryServiceId) : null,
        Severity = request.Severity,
        Status = "open",
        Title = request.Title,
        CreatedAt = DateTime.UtcNow,
        DetectedAt = request.DetectedAtUnixMs > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(request.DetectedAtUnixMs).UtcDateTime 
            : DateTime.UtcNow,
        ResolvedAt = (DateTime?)null,
        CreatedBy = request.CreatedBy ?? tenantContext.UserId,
        Assignee = (string?)null,
        Labels = request.Labels ?? new Dictionary<string, string>()
    };

    // TODO: Use proper entity framework entities
    // For now, using raw SQL for MVP
    await db.Database.ExecuteSqlRawAsync(@"
        INSERT INTO incidents (id, tenant_id, env_id, primary_service_id, severity, status, title, 
                              created_at, detected_at, created_by, labels)
        VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}::jsonb)",
        incident.Id, incident.TenantId, incident.EnvId, incident.PrimaryServiceId, 
        incident.Severity, incident.Status, incident.Title, incident.CreatedAt, 
        incident.DetectedAt, incident.CreatedBy, JsonSerializer.Serialize(incident.Labels));

    // Create initial event
    await db.Database.ExecuteSqlRawAsync(@"
        INSERT INTO incident_events (id, tenant_id, incident_id, type, payload)
        VALUES (gen_random_uuid(), {0}, {1}, 'detection', {2}::jsonb)",
        incident.TenantId, incident.Id, JsonSerializer.Serialize(new { source = "manual" }));

    return Results.Created($"/incidents/{incident.Id}", incident);
})
.WithName("CreateIncident")
.WithOpenApi();

app.MapGet("/incidents/{id}", async (
    HttpContext context,
    IncidentScopeDbContext db,
    string id) =>
{
    var tenantContext = context.GetTenantContext();
    var tenantId = Guid.Parse(tenantContext.TenantId);
    var incidentId = Guid.Parse(id);

    // TODO: Use proper entity queries with FromSqlRaw
    // For MVP, using direct SQL query with ADO.NET
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();
    using var command = connection.CreateCommand();
    command.CommandText = @"
        SELECT id, tenant_id, env_id, primary_service_id, severity, status, title,
               created_at, detected_at, resolved_at, created_by, assignee, labels::text
        FROM incidents
        WHERE id = $1 AND tenant_id = $2";
    var param1 = new NpgsqlParameter { ParameterName = "$1", Value = incidentId };
    var param2 = new NpgsqlParameter { ParameterName = "$2", Value = tenantId };
    command.Parameters.Add(param1);
    command.Parameters.Add(param2);
    
    using var reader = await command.ExecuteReaderAsync();
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

    var query = "SELECT * FROM incidents WHERE tenant_id = {0}";
    var parameters = new List<object> { tenantId };

    if (!string.IsNullOrEmpty(envId))
    {
        query += " AND env_id = {" + parameters.Count + "}";
        parameters.Add(Guid.Parse(envId));
    }

    if (!string.IsNullOrEmpty(status))
    {
        query += " AND status = {" + parameters.Count + "}";
        parameters.Add(status);
    }

    if (severity.HasValue)
    {
        query += " AND severity = {" + parameters.Count + "}";
        parameters.Add(severity.Value);
    }

    query += " ORDER BY created_at DESC LIMIT 100";

    // For MVP, using ADO.NET directly
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();
    using var command = connection.CreateCommand();
    command.CommandText = query;
    for (int i = 0; i < parameters.Count; i++)
    {
        var param = command.CreateParameter();
        param.ParameterName = $"${i + 1}";
        param.Value = parameters[i];
        command.Parameters.Add(param);
    }

    var incidents = new List<IncidentDto>();
    using var reader = await command.ExecuteReaderAsync();
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

