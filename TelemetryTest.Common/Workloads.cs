namespace TelemetryTest.Common;

public static class Workloads
{
    public static List<int> CalculateFactors(int number, CancellationToken cancellationToken = default)
    {
        var factors = new List<int>();
        int max = (int)Math.Sqrt(number);

        for (int factor = 1; factor <= max; ++factor)
        {
            if (number % factor != 0)
                continue;

            factors.Add(factor);
            if (factor != number / factor)
                factors.Add(number / factor);
            
            cancellationToken.ThrowIfCancellationRequested();
        }

        return factors;
    }
    
}