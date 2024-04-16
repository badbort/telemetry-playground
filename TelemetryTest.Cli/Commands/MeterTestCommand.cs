using System.Diagnostics.Metrics;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using TelemetryTest.Common;

namespace TelemetryTest.Cli.Commands;

[Command("meter-test")]
public class MeterTestCommand(IConsole console, ILogger<MeterTestCommand> logger) : BaseAsyncCommand(console, logger)
{
    public override async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken)
    {
        var factorsCounter = Program.Meter.CreateCounter<long>("FactorIterations");
        var primesCounter = Program.Meter.CreateUpDownCounter<long>("Primes");
        var histogram = Program.Meter.CreateHistogram<long>("Factors");
        
        int i = 4;
        int primeCount = 0;

        // ReSharper disable once AccessToModifiedClosure
        Program.Meter.CreateObservableCounter("observableCounter", () => primeCount);

        do
        {
            var factors =  Workloads.CalculateFactors(i);
            histogram.Record(factors.Count, new KeyValuePair<string, object?>("Input", i));

            factorsCounter.Add(1);

            if (factors.Count == 2)
            {
                primesCounter.Add(1);
                Logger.LogInformation($"Found prime #{++primeCount}: {i}");
            }

            if (i % 100 == 0)
            {
                Logger.LogInformation($"Found {factors.Count} factors for {i}");
            }

            i++;
            
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(5);
        } while (!cancellationToken.IsCancellationRequested);
        
        Logger.LogInformation($"Stopped after calculating factors for numbers up to {i}. Primes found: {primeCount}");
    }
}