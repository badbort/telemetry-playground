using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace TelemetryTest.Cli.Commands;

/// <summary>
/// Base class for synchronous commands
/// </summary>
public abstract class BaseCommand
{
    protected ILogger Logger { get; }

    protected IConsole Console { get; }

    protected BaseCommand(IConsole console, ILogger logger)
    {
        Console = console;
        Logger = logger;
    }

    public abstract void OnExecute(CommandLineApplication app, CancellationToken cancellationToken);
}