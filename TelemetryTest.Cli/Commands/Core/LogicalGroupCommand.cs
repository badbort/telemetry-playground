using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace TelemetryTest.Cli.Commands;

/// <summary>
/// A base command type for commands that only serve to group other subcommands together. When execute they will simply
/// print the command help text to console.
/// </summary>
public abstract class LogicalGroupCommand : BaseCommand
{
    protected LogicalGroupCommand(IConsole console, ILogger logger) : base(console, logger)
    {
    }

    public override void OnExecute(CommandLineApplication app, CancellationToken cancellationToken) => app.ShowHelp();
}