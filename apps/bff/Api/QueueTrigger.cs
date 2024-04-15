using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using TelemetryTest.AzureStorage;
using TelemetryTest.AzureStorage.Queues;

namespace TelemetryTest.Bff.Api;

public class QueueTrigger
{
    private readonly ILogger<QueueTrigger> _logger;

    public QueueTrigger(ILogger<QueueTrigger> logger) => _logger = logger;

    [Function(nameof(QueueTrigger))]
    [Singleton(Mode = SingletonMode.Function)]
    public async Task Run([QueueTrigger("testqueue")] TracedQueueMessage job)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {job.Name}");

        var activityLinks = new List<ActivityLink>();
        if (ActivityContext.TryParse(job.TraceParent, job.TraceState, true, out var linkedContext))
        {
            activityLinks.Add(new ActivityLink(linkedContext));
        }

        using (var _ = Program.ActivitySource.StartActivity(name: "Simulated Prepwork", kind: ActivityKind.Consumer, links: activityLinks))
        {
            await Task.Delay(1000);
        }

        using (var activity = Program.ActivitySource.StartActivity(name: "Calculate Factors", kind: ActivityKind.Consumer, links: activityLinks))
        {
            _logger.LogInformation($"Starting simulated workload with {job.Input}");

            activity?.AddTag($"job.{nameof(job.Input)}", job.Input);

            var factors = CalculateFactors(job.Input);

            activity?.AddTag("job.factors.count", factors.Count);
            activity?.AddTag("job.factors", factors.ToArray());
            _logger.LogInformation($"Found {factors.Count} factors from input parameter {job.Input}");
        }

        _logger.LogInformation("Did things");
    }

    /// <summary>
    /// https://stackoverflow.com/a/239877
    /// </summary>
    private List<int> CalculateFactors(int number)
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
        }

        return factors;
    }
}