using System.Diagnostics;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using TelemetryTest.Common.Jobs;

namespace TelemetryTest.AzureStorage.Jobs;

public class JobEntity : ITableEntity
{
    public ActivityContext SourceActivity { get; set; }
    
    public bool Complete { get; set; }
    public string Result { get; set; }
    
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public class QueueJobSchedulerOptions
{
    public string StorageConnectionString { get; set; }
    
    public string QueueName { get; set; }
    
    public string TableName { get; set; }
}

public class QueueJobScheduler : IBigJobScheduler
{
    private readonly QueueClient _queueClient;
    private readonly TableClient _tableClient;

    public QueueJobScheduler(QueueJobSchedulerOptions options)
    {
        var queueServiceClient = new QueueServiceClient(options.StorageConnectionString);
        var tableServiceClient = new TableServiceClient(options.StorageConnectionString);

        queueServiceClient.CreateQueue(options.QueueName);
        tableServiceClient.CreateTable(options.TableName);

        _queueClient = queueServiceClient.GetQueueClient(options.QueueName);
        _tableClient = tableServiceClient.GetTableClient(options.TableName);
    }

    public async Task<string> SubmitJob<T>(T job) where T : Job
    {
        var jobId = Guid.NewGuid().ToString();
        var message = JsonConvert.SerializeObject(job);
        await _queueClient.SendMessageAsync(message);

        var jobEntity = new JobEntity
        {
            PartitionKey = "Job",
            RowKey = jobId,
        };

        await _tableClient.AddEntityAsync(jobEntity);
        return jobId;
    }

    private async Task<JobEntity> GetJob(string jobId)
    {
        return await _tableClient.GetEntityAsync<JobEntity>("Job", jobId);
    }

    public async Task<bool> GetJobState(string jobId)
    {
        var job = await GetJob(jobId);
        return job.Complete;
    }

    public async Task CancelJob(string jobId)
    {
        throw new NotImplementedException();
        // var retrieveOperation = TableOperation.Retrieve<JobEntity>("Job", jobId);
        // var retrievedResult = await jobTable.ExecuteAsync(retrieveOperation);
        // var deleteEntity = (JobEntity)retrievedResult.Result;
        // if (deleteEntity != null)
        // {
        //     var deleteOperation = TableOperation.Delete(deleteEntity);
        //     await jobTable.ExecuteAsync(deleteOperation);
        // }
    }

    public async Task<T?> GetJobResult<T>(string jobId)
    {
        var jobEntity = await GetJob(jobId);
        return JsonConvert.DeserializeObject<T>(jobEntity.Result);
    }
}