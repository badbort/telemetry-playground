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
[Subcommand(typeof(MeterTestCommand))]
public class RootCommand(IConsole console, ILogger<RootCommand> logger) : LogicalGroupCommand(console, logger);