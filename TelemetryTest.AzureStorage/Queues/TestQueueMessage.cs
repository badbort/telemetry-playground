namespace TelemetryTest.AzureStorage.Queues;

public class TracedQueueMessage
{
    public int Input { get; set; }

    public string? TraceParent { get; set; }
    
    public string? TraceState { get; set; }

    public string? Name { get; set; }
}