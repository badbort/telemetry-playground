namespace TelemetryTest.Common.Jobs;

public class CalculatePi : Job<double>
{
    public int Iterations { get; set; }
}