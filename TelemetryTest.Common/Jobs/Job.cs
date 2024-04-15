namespace TelemetryTest.Common.Jobs;

public abstract class Job
{
    public object? Result { get; set; }

    public bool IsError { get; set; }
    
    public abstract Type ResultType { get; }
}

public abstract class Job<T> : Job
{
    public new T? Result { get; set; }

    public override Type ResultType => typeof(T);
}