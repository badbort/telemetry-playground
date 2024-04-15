using Microsoft.Extensions.Configuration;

namespace TelemetryTest.Common.Configuration;

public class FunctionAppSettings
{
    [ConfigurationKeyName("WEBSITE_SLOT_NAME")]
    public string? SlotName { get; set; }
    
    [ConfigurationKeyName("WEBSITE_SITE_NAME")]
    public string? SiteName { get; set; }

    [ConfigurationKeyName("AzureWebJobsStorage")]
    public string? StorageConnectionString { get; set; }

    [ConfigurationKeyName("APPLICATIONINSIGHTS_CONNECTION_STRING ")]
    public string? ApplicationInsightsConnectionString { get; set; }
}