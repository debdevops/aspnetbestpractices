using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;
using Azure.Monitor.OpenTelemetry.Exporter;

namespace Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetryConfigured(this IServiceCollection services, IConfiguration config)
    {
        var serviceName    = config["Observability:ServiceName"] ?? "AspNetBestPractice.Api";
        var serviceVersion = config["Observability:ServiceVersion"] ?? "1.0.0";
        var connString     = config["Observability:AzureMonitor:ConnectionString"];

        services.Configure<OpenTelemetryLoggerOptions>(opt =>
        {
            opt.IncludeScopes = true;
            opt.ParseStateValues = true;
        });

        services.AddOpenTelemetry()
            .ConfigureResource(rb => rb.AddService(serviceName, serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                    .AddHttpClientInstrumentation();
                // tracing runtime isn’t a thing; only metrics:
                if (!string.IsNullOrWhiteSpace(connString))
                    tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = connString);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
                if (!string.IsNullOrWhiteSpace(connString))
                    metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = connString);
            });

        if (!string.IsNullOrWhiteSpace(connString))
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(o =>
                {
                    o.IncludeFormattedMessage = true;
                    o.IncludeScopes = true;
                    o.AddAzureMonitorLogExporter(exp => exp.ConnectionString = connString);
                });
            });
        }

        return services;
    }
}
