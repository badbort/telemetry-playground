using System.Diagnostics;
using System.Dynamic;
using Azure.Storage.Queues;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using TelemetryTest.AzureStorage;
using TelemetryTest.AzureStorage.Queues;

namespace TelemetryTest.Cli.Commands;

[Command("queue-job")]
public class QueueJobCommand(IConsole console, IOptions<Settings> appSettings, ILogger<QueueJobCommand> logger)
    : BaseAsyncCommand(console, logger)
{
    private readonly Settings _appSettings = appSettings.Value;

    [Option] 
    public string? StorageConnectionString { get; set; }

    [Option] 
    public string Queue { get; set; } = "testqueue";
    
    [Option]
    public int Input { get; set; }

    public override async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Running command with input {Input}");
        StorageConnectionString ??= _appSettings.StorageConnectionString;
        
        using var activity = Program.ActivitySource.StartActivity(nameof(QueueJobCommand));
        activity?.SetTag($"command.{nameof(Input)}", Input);
            
        Logger.LogInformation($"Executing job command with input {Input}");
        
        var queueServiceClient = new QueueServiceClient(StorageConnectionString, new QueueClientOptions {MessageEncoding = QueueMessageEncoding.Base64});
        await queueServiceClient.CreateQueueAsync(Queue, cancellationToken: cancellationToken);
        var queueClient = queueServiceClient.GetQueueClient(Queue);

        var queueItem = new TracedQueueMessage();
        queueItem.Name = "Fred";
        queueItem.Input = Input;
        
        queueItem.TraceState = Activity.Current?.Context.TraceState;
        queueItem.TraceParent = Activity.Current?.Id;
        
        // Send the item
        var response = await queueClient.SendMessageAsync(BinaryData.FromObjectAsJson(queueItem), cancellationToken: cancellationToken);
        activity?.SetTag("job.MessageId", response.Value.MessageId);
        
        Logger.LogInformation($"Job was successfully dispatched with input {Input} and returned job id {response.Value.MessageId}");
    }
}