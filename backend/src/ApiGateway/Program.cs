using IncidentScope.Observability;
using IncidentScope.Security.Tenant;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["CORS_ORIGINS"]?.Split(',') ?? new[] { "http://localhost:3000" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Observability
builder.Services.AddIncidentScopeObservability(
    serviceName: "api-gateway",
    serviceVersion: "1.0.0",
    otlpEndpoint: builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

// Tenant middleware is registered via UseMiddleware, no need to register here

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<TenantMiddleware>();
app.UseHttpsRedirection();

// Get incident service URL from config
var incidentServiceUrl = builder.Configuration["Services:IncidentService"] 
    ?? "http://localhost:5001";

// BFF endpoints - aggregate data from backend services
app.MapGet("/api/incidents", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    var tenantContext = context.GetTenantContext();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-tenant-id", tenantContext.TenantId);

    var response = await client.GetAsync($"{incidentServiceUrl}/incidents");
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync();
    var incidents = JsonSerializer.Deserialize<List<object>>(content);

    return Results.Ok(incidents);
})
.WithName("ListIncidents")
.WithOpenApi();

app.MapGet("/api/incidents/{id}", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    string id) =>
{
    var tenantContext = context.GetTenantContext();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-tenant-id", tenantContext.TenantId);

    var response = await client.GetAsync($"{incidentServiceUrl}/incidents/{id}");
    
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var incident = JsonSerializer.Deserialize<object>(content);

    return Results.Ok(incident);
})
.WithName("GetIncident")
.WithOpenApi();

app.MapGet("/api/incidents/{id}/overview", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    string id) =>
{
    var tenantContext = context.GetTenantContext();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-tenant-id", tenantContext.TenantId);

    // Aggregate incident + timeline + hypotheses
    var incidentTask = client.GetAsync($"{incidentServiceUrl}/incidents/{id}");
    
    // TODO: Add timeline and hypotheses endpoints when correlation engine is ready
    await incidentTask;
    
    var incidentResponse = await incidentTask;
    if (incidentResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    var incidentContent = await incidentResponse.Content.ReadAsStringAsync();
    var incident = JsonSerializer.Deserialize<object>(incidentContent);

    var overview = new
    {
        Incident = incident,
        Timeline = new List<object>(), // TODO: Fetch from incident-service
        Hypotheses = new List<object>(), // TODO: Fetch from correlation-engine
        SuspectServices = new List<object>() // TODO: Fetch from correlation-engine
    };

    return Results.Ok(overview);
})
.WithName("GetIncidentOverview")
.WithOpenApi();

app.MapPost("/api/incidents", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    CreateIncidentRequest request) =>
{
    var tenantContext = context.GetTenantContext();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-tenant-id", tenantContext.TenantId);

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var response = await client.PostAsync($"{incidentServiceUrl}/incidents", content);
    response.EnsureSuccessStatusCode();

    var responseContent = await response.Content.ReadAsStringAsync();
    var incident = JsonSerializer.Deserialize<object>(responseContent);

    return Results.Created($"/api/incidents/{incident}", incident);
})
.WithName("CreateIncident")
.WithOpenApi();

app.MapPost("/api/incidents/{id}/resolve", async (
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    string id,
    ResolveIncidentRequest request) =>
{
    var tenantContext = context.GetTenantContext();
    var client = httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("x-tenant-id", tenantContext.TenantId);

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

    var response = await client.PostAsync($"{incidentServiceUrl}/incidents/{id}/resolve", content);
    
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }

    response.EnsureSuccessStatusCode();
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

