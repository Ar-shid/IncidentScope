using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace IncidentScope.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddIncidentScopeObservability(
        this IServiceCollection services,
        string serviceName,
        string serviceVersion,
        string? otlpEndpoint = null)
    {
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.name"] = serviceName,
                ["service.version"] = serviceVersion
            });

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(serviceName);

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    builder.AddConsoleExporter();
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
                else
                {
                    builder.AddConsoleExporter();
                }
            });

        // Note: Logging to OpenTelemetry requires OpenTelemetry.Extensions.Logging package
        // which is not included in the current setup. Standard .NET logging will work.
        // To enable OTel logging, add the package and uncomment:
        // services.AddLogging(builder => builder.AddOpenTelemetry(...));

        return services;
    }
}

