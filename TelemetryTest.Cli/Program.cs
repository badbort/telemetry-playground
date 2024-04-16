using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    private const string AppName = "TelemetryTest.Cli";
    internal static readonly ActivitySource ActivitySource = new(AppName);
    internal static readonly Meter Meter = new(AppName);

    public static async Task<int> Main(string[] args)
    {
        var builder = new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddJsonFile("appsettings.json");
                config.AddUserSecrets<Program>();
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.Configure(o =>
                {
                    o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId;
                });
                
                var telemetrySettings = hostContext.Configuration.GetSection(TelemetryConfiguration.ConfigSection).Get<TelemetryConfiguration>();

                logging.AddSimpleConsole(o =>
                {
                    o.IncludeScopes = true;
                    o.TimestampFormat = "hh:mm:ss ";
                    o.SingleLine = true;
                });
                logging.SetMinimumLevel(LogLevel.Warning);
                logging.AddFilter("TelemetryTest", LogLevel.Debug);
                
                
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
                        builder.AddAttributes(new Dictionary<string, object>
                        {
                            { "service.name", AppName },
                            { "service.namespace", typeof(Program).Assembly.GetName().Name! }
                        });
                        
                        builder.AddService(serviceName: AppName);
                    })
                    .WithTracing(builder =>
                    {
                        builder.AddSource(ActivitySource.Name);
                        builder.SetSampler<AlwaysOnSampler>();

                        builder.AddHttpClientInstrumentation();
                        
                        if (!string.IsNullOrEmpty(telemetrySettings?.ApplicationInsightsConnectionString))
                            builder.AddAzureMonitorTraceExporter(o => o.ConnectionString = telemetrySettings.ApplicationInsightsConnectionString);
                        
                        if (!string.IsNullOrEmpty(telemetrySettings?.OtlpEndpoint))
                            builder.AddOtlpExporter(o => o.Endpoint = new Uri(telemetrySettings.OtlpEndpoint));

                    })
                    .WithMetrics(builder =>
                    {
                        builder.AddMeter(Meter.Name);
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