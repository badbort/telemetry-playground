using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TelemetryTest.Common.Configuration;

namespace TelemetryTest.Bff;

public static class Program
{
    internal const string AppName = "TelemetryTest-Bff";
    internal static readonly ActivitySource ActivitySource = new(AppName);
    
    public static void Main(string[] args)
    {
        var resourceAttributes = new Dictionary<string, object>
        {
            { "service.name", AppName },
            { "service.namespace", typeof(Program).Assembly.GetName().Name! }
        };
        
        var host = new HostBuilder() 
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddUserSecrets(typeof(Program).Assembly);
            })
            .ConfigureLogging((context, logging) =>
            {
                var functionAppSettings = context.Configuration.Get<FunctionAppSettings>();
                var telemetry = context.Configuration.Get<TelemetryConfiguration>();

                logging.Configure(o =>
                {
                    o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
                });

                logging.ClearProviders();
                logging.AddOpenTelemetry(builder =>
                {
                    builder.IncludeScopes = true;
                    builder.IncludeFormattedMessage = true;

                    if (!string.IsNullOrEmpty(functionAppSettings?.ApplicationInsightsConnectionString))
                        builder.AddAzureMonitorLogExporter(o => o.ConnectionString = functionAppSettings.ApplicationInsightsConnectionString);

                    if (!string.IsNullOrEmpty(telemetry?.OtlpEndpoint))
                        builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetry.OtlpEndpoint));
                });

                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            } )
            .ConfigureServices((context, services) =>
            {
                var functionAppSettings = context.Configuration.Get<FunctionAppSettings>();
                var telemetry = context.Configuration.GetSection(TelemetryConfiguration.ConfigSection).Get<TelemetryConfiguration>();
                
                services.AddSingleton<IOpenApiHttpTriggerAuthorization>(_ =>
                {
                    var auth = new OpenApiHttpTriggerAuthorization(req =>
                    {
                        var result = default(OpenApiAuthorizationResult);
                        return Task.FromResult(result);
                    });

                    return auth;

                });
                services.AddHttpClient();
                
                services.AddOpenTelemetry()
                    .ConfigureResource(builder =>
                    {
                        builder.AddAttributes(resourceAttributes);
                        builder.AddService(serviceName: AppName);
                    })
                    .WithTracing(builder =>
                    {
                        builder.AddSource(ActivitySource.Name);
                        builder.AddHttpClientInstrumentation();
                        builder.SetSampler<AlwaysOnSampler>();
                        // Not sure if this is needed
                        builder.AddSource("Azure.*");
                        builder.AddSource("Microsoft.Azure.Functions.Worker");

                        if (!string.IsNullOrEmpty(functionAppSettings?.ApplicationInsightsConnectionString))
                            builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = functionAppSettings.ApplicationInsightsConnectionString);
                        
                        if (!string.IsNullOrEmpty(telemetry?.OtlpEndpoint))
                            builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetry.OtlpEndpoint));

                    })
                    .WithMetrics(builder =>
                    {
                        //builder.AddMeter(MeterName);
                        builder.AddHttpClientInstrumentation();
                        if (!string.IsNullOrEmpty(functionAppSettings?.ApplicationInsightsConnectionString))
                            builder.AddAzureMonitorMetricExporter(o => o.ConnectionString = functionAppSettings.ApplicationInsightsConnectionString);
                        
                        if (!string.IsNullOrEmpty(telemetry?.OtlpEndpoint))
                            builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetry.OtlpEndpoint));
                    });
            })
            
            .Build();

        host.Run();
    }
}