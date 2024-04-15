using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace TelemetryTest.Cli.Commands;

/// <summary>
/// Root level command that is executed when the program is run without any subcommand.
/// </summary>
[Command]
[UsedImplicitly]
[Subcommand(typeof(QueueJobCommand))]
public class RootCommand : LogicalGroupCommand
{
    public RootCommand(IConsole console, ILogger<RootCommand> logger) : base(console, logger)
    {
    }
}