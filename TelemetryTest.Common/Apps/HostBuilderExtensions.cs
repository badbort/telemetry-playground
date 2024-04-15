using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TelemetryTest.Common.Configuration;

namespace TelemetryTest.Common.Apps;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddOtel(this IHostBuilder hostBuilder, string appName, Dictionary<string, object>? resourceAttributes = null)
    {
        
        return hostBuilder.ConfigureServices((context, services) =>
        {
            var telemetry = context.Configuration.GetSection(TelemetryConfiguration.ConfigSection).Get<TelemetryConfiguration>();
            var otlpEndpoint = telemetry?.OtlpEndpoint ?? "http://localhost:4317";
            
            services.AddOpenTelemetry()
                .ConfigureResource(builder =>
                {
                    if(resourceAttributes != null)
                        builder.AddAttributes(resourceAttributes);
                    
                    builder.AddService(serviceName: appName);
                })
                .WithTracing(builder =>
                {
                    builder.AddSource(appName);
                    builder.AddHttpClientInstrumentation();
                    builder.SetSampler<AlwaysOnSampler>();

                    if (!string.IsNullOrEmpty(telemetry?.ApplicationInsightsConnectionString))
                        builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = telemetry.ApplicationInsightsConnectionString);

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                        builder.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                })
                .WithMetrics(builder =>
                {
                    //builder.AddMeter(MeterName);
                    builder.AddHttpClientInstrumentation();
                    if (!string.IsNullOrEmpty(telemetry?.ApplicationInsightsConnectionString))
                        builder.AddAzureMonitorMetricExporter(o => o.ConnectionString = telemetry.ApplicationInsightsConnectionString);

                    if (!string.IsNullOrEmpty(otlpEndpoint))
                        builder.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                });
        });
    }
}