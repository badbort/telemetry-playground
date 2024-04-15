using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace TelemetryTest.Cli.Commands;

public class LocalWorkloadCommand : BaseAsyncCommand
{
    
    public LocalWorkloadCommand(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    public override async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken)
    {
        
    }
}