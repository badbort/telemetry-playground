using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace TelemetryTest.Cli.Commands;

/// <summary>
/// Base class for commands that execute asynchronously.
/// </summary>
public abstract class BaseAsyncCommand
{
    protected ILogger Logger { get; }

    protected IConsole Console { get; }
    
    [Option("--otlp", Description = "Optional open telemetry protocol endpoint to export traces and logs")]
    public string? Otlp { get; set; }

    protected BaseAsyncCommand(IConsole console, ILogger logger)
    {
        Console = console;
        Logger = logger;
    }

    /// <summary>
    /// The method invoked when the command line app executes this command.
    /// </summary>
    /// <param name="app">
    /// The <see cref="CommandLineApplication"/> instance associated with this command model. This contains additional information about the command
    /// execution including additional uncaptured arguments (if enabled).
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    public abstract Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken);
}