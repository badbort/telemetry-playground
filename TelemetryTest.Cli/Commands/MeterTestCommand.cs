using System.Diagnostics;
using System.Diagnostics.Metrics;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using TelemetryTest.Common;

namespace TelemetryTest.Cli.Commands;

[Command("meter-test")]
public class MeterTestCommand(IConsole console, ILogger<MeterTestCommand> logger) : BaseAsyncCommand(console, logger)
{
    [Option("--delay", Description = "Delay in ms between calculation")]
    public int? Delay { get; set; }

    [Option("--events")]
    public bool CreateEvents { get; set; }
    
    [Option("--max")]
    public int? Max { get; set; }

    public override async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken)
    {
        using var activity = Program.ActivitySource.StartActivity(nameof(MeterTestCommand));
        activity?.SetTag($"command.{nameof(Delay)}", Delay);
        activity?.SetTag($"command.{nameof(CreateEvents)}", CreateEvents);

        var factorsCounter = Program.Meter.CreateCounter<long>("FactorCalculations");
        var primesCounter = Program.Meter.CreateUpDownCounter<long>("Primes");
        var histogram = Program.Meter.CreateHistogram<long>("Factors");

        int i = 4;
        int primeCount = 0;

        // ReSharper disable once AccessToModifiedClosure
        Program.Meter.CreateObservableCounter("observableCounter", () => primeCount);

        do
        {
            var factors = Workloads.CalculateFactors(i);
            histogram.Record(factors.Count, new KeyValuePair<string, object?>("Input", i));

            factorsCounter.Add(1);

            if (factors.Count == 2)
            {
                primesCounter.Add(1);
                Logger.LogInformation($"Found prime #{++primeCount}: {i}");
                CreateEventIfNecessary(activity, i, primeCount);
            }

            if (i % 100 == 0)
            {
                Logger.LogInformation($"Found {factors.Count} factors for {i}");
            }

            i++;

            // ReSharper disable once MethodSupportsCancellation

            if (Delay.HasValue)
                await Task.Delay(Delay.Value);
            
            if(Max.HasValue && Max < i)
                break;
            
        } while (!cancellationToken.IsCancellationRequested);
        
        activity?.Stop();
        Logger.LogInformation($"Stopped after calculating factors for numbers up to {i}. Primes found: {primeCount}");
    }

    private void CreateEventIfNecessary(Activity? activity, int prime, int nth)
    {
        if (!CreateEvents) 
            return;
        
        activity?.AddEvent(new ActivityEvent("Discovered Prime", tags: new ActivityTagsCollection
        {
            { "Value", prime },
            { "Nth", nth }
        }));
    }
}