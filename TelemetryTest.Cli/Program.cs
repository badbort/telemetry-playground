using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TelemetryTest.Cli.Commands;
using TelemetryTest.Common.Configuration;

namespace TelemetryTest.Cli;

class Program
{
    public static ActivitySource ActivitySource = new(AppName);
    public const string AppName = "TelemetryTest.Cli";

    public static async Task<int> Main(string[] args)
    {
        var resourceAttributes = new Dictionary<string, object>
        {
            { "service.name", AppName },
            { "service.namespace", typeof(Program).Assembly.GetName().Name! }
        };

        var builder = new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json");
                config.AddUserSecrets<Program>();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                var telemetrySettings = hostContext.Configuration.GetValue<TelemetryConfiguration>(TelemetryConfiguration.ConfigSection);

                logging.AddSimpleConsole(o =>
                {
                    o.IncludeScopes = true;
                    o.TimestampFormat = "hh:mm:ss ";
                    o.SingleLine = true;
                });
                logging.SetMinimumLevel(LogLevel.Warning);
                logging.AddFilter("TelemetryTest", LogLevel.Debug);
                
                logging.Configure(o =>
                {
                    o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
                });
                
                // Send logs using the OpenTelemetry logger provider
                logging.AddOpenTelemetry(builder =>
                {
                    builder.IncludeScopes = true;
                    builder.IncludeFormattedMessage = true;

                    if (!string.IsNullOrEmpty(telemetrySettings?.ApplicationInsightsConnectionString))
                        builder.AddAzureMonitorLogExporter(o => o.ConnectionString = telemetrySettings.ApplicationInsightsConnectionString);

                    if (!string.IsNullOrEmpty(telemetrySettings?.OtlpEndpoint))
                        builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetrySettings.OtlpEndpoint));
                });

                logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Bind to root config
                services.AddOptions<Settings>().BindConfiguration(string.Empty);
                services.AddOptions<TelemetryConfiguration>().BindConfiguration(TelemetryConfiguration.ConfigSection);
                var telemetrySettings = hostContext.Configuration.GetSection(TelemetryConfiguration.ConfigSection).Get<TelemetryConfiguration>();
                
                services.AddSingleton(PhysicalConsole.Singleton);
                
                services.AddOpenTelemetry()
                    .ConfigureResource(builder =>
                    {
                        builder.AddAttributes(resourceAttributes);
                        builder.AddService(serviceName: AppName);
                    })
                    .WithTracing(builder =>
                    {
                        //builder.AddSource(ActivitySource.Name);
                        builder.AddHttpClientInstrumentation();
                        builder.SetSampler<AlwaysOnSampler>();
                        
                        // Not sure if this is needed
                        builder.AddSource("Azure.*");
                        builder.AddSource("Microsoft.Azure.Functions.Worker");

                        if (!string.IsNullOrEmpty(telemetrySettings?.ApplicationInsightsConnectionString))
                            builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = telemetrySettings.ApplicationInsightsConnectionString);
                        
                        if (!string.IsNullOrEmpty(telemetrySettings?.OtlpEndpoint))
                            builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetrySettings.OtlpEndpoint));

                    })
                    .WithMetrics(builder =>
                    {
                        //builder.AddMeter(MeterName);
                        builder.AddHttpClientInstrumentation();
                        if (!string.IsNullOrEmpty(telemetrySettings?.ApplicationInsightsConnectionString))
                            builder.AddAzureMonitorMetricExporter(o => o.ConnectionString = telemetrySettings.ApplicationInsightsConnectionString);
                        
                        if (!string.IsNullOrEmpty(telemetrySettings?.OtlpEndpoint))
                            builder.AddOtlpExporter(o =>
                            {
                                o.Endpoint = new Uri(telemetrySettings.OtlpEndpoint);
                            });
                    });
            });

        return await builder.RunCommandLineApplicationAsync<RootCommand>(args);
    }
}