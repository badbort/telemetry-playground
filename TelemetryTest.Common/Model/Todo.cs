namespace TelemetryTest.Common.Model;

public class TodoItem
{
    public Guid Guid { get; set; }
    
    public string? Message { get; set; }
    
    public DateTimeOffset Created { get; set; }
    
    public DateTimeOffset LastModified { get; set; }

    public string Topic { get; set; } = string.Empty;
    
    public List<string>? Tags { get; set; }
    
    public bool Hidden { get; set; }
}