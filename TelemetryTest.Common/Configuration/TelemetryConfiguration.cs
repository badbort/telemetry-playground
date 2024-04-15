namespace TelemetryTest.Common.Configuration;

public class TelemetryConfiguration
{
    public const string ConfigSection = "Telemetry";
    
    /// <summary>
    /// Optional open telemetry protocol endpoint to export to
    /// </summary>
    public string? OtlpEndpoint { get; set; }
    
    /// <summary>
    /// Optional app insights connection string to export to.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; set; }
}