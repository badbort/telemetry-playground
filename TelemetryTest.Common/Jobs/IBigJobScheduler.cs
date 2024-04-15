namespace TelemetryTest.Common.Jobs;

public interface IBigJobScheduler
{
    Task<bool> GetJobState(string jobId);

    Task<string> SubmitJob<T>(T job) where T : Job;

    Task CancelJob(string jobId);

    Task<T?> GetJobResult<T>(string jobId);
}