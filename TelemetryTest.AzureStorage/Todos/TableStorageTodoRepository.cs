using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using TelemetryTest.Common.Data;
using TelemetryTest.Common.Model;

namespace TelemetryTest.AzureStorage.Todos;

public class TableStorageTodoRepositorySettings
{
    public string StorageConnectionString { get; set; }

    public string TodosTable { get; set; } = "todos";

    public string TagsTable { get; set; } = "todostags";

    public string TopicsTable { get; set; } = "todostopics";

    public string ListsTable { get; set; } = "lists";
}

public class TableStorageTodoRepository : ITodoRepository
{
    private readonly TableStorageTodoRepositorySettings _settings;
    private readonly ILogger<TableStorageTodoRepository> _logger;
    private readonly Lazy<Task<TableServiceClient>> _lazyInit;
    
    private TableClient _listsClient;
    private TableClient _topicsClient;
    private TableClient _tagsClient;
    private TableClient _todosClient;

    public TableStorageTodoRepository(TableStorageTodoRepositorySettings settings, ILogger<TableStorageTodoRepository> logger)
    {
        _settings = settings;
        _logger = logger;

        _lazyInit = new Lazy<Task<TableServiceClient>>(async () =>
        {
            var serviceClient = new TableServiceClient(_settings.StorageConnectionString);
            await serviceClient.CreateTableIfNotExistsAsync(settings.TodosTable);
            await serviceClient.CreateTableIfNotExistsAsync(settings.TagsTable);
            await serviceClient.CreateTableIfNotExistsAsync(settings.TopicsTable);

            _todosClient = serviceClient.GetTableClient(settings.TodosTable);
            _tagsClient = serviceClient.GetTableClient(settings.TagsTable);
            _topicsClient =  serviceClient.GetTableClient(settings.TopicsTable);
            _listsClient = serviceClient.GetTableClient(settings.ListsTable);
            
            return serviceClient;
        });
    }

    public async Task<Guid> AddTodo(TodoItem todo)
    {
        await InitializeIfNecessary();
        
        if (todo.Guid == default)
            todo.Guid = Guid.NewGuid();

        var todoEntity = new TodoTableEntity();
        todoEntity.UpdateTableEntityValues(todo);
        await _todosClient.AddEntityAsync(todoEntity);

        foreach (string tag in todo.Tags ?? Enumerable.Empty<string>())
        {
            await AddLookupEntity(_tagsClient, tag, todo.Guid);
            await AddListItem("topics", tag);
        }

        await AddLookupEntity(_topicsClient, todo.Topic, todo.Guid);
        await AddListItem("topics", todo.Topic);
        
        return todo.Guid;
    }

    private Task AddListItem(string listName, string item) => _listsClient.UpsertEntityAsync(new TableEntity(listName, item));

    public async Task<bool> Exists(Guid id)
    {
        await _lazyInit.Value;
        var obj = await _todosClient.GetEntityIfExistsAsync<TodoTableEntity>(string.Empty, id.ToString("D"), Array.Empty<string>());
        return obj != null;
    }

    public async Task<List<TodoItem>> GetLatestTodos(int count)
    {
        return await _todosClient.QueryAsync<TodoTableEntity>().Take(count).Select( e =>
        {
            e.UpdateTableEntityValues();
            return e;
        }).OfType<TodoItem>().ToListAsync();
    }

    public Task<List<string>> GetTags() => GetList("tags");


    public Task<List<string>> GetTopics() => GetList("topics");
    
    public Task<List<TodoItem>> FromTopic(string topic, int count) => EnumerateByLookup(_tagsClient, topic).Take(count).ToListAsync().AsTask();

    public Task<List<TodoItem>> FromTag(string tag, int count) => EnumerateByLookup(_tagsClient, tag).Take(count).ToListAsync().AsTask();

    private async IAsyncEnumerable<TodoItem> EnumerateByLookup(TableClient tableClient, string partitionKey)
    {
        await foreach (var tableEntity in tableClient.QueryAsync<TableEntity>(e => e.PartitionKey == partitionKey))
        {
            var guid = Guid.Parse(tableEntity.RowKey);
            var todoItem = await Get(guid);
            if (todoItem != null)
                yield return todoItem;
        }
    }

    private Task<List<string>> GetList(string listName) => _listsClient.QueryAsync<TableEntity>(e => e.PartitionKey == listName).Select(e => e.RowKey).ToListAsync().AsTask();

    public async IAsyncEnumerable<TodoItem> GetFromTags(string tag)
    {
        await InitializeIfNecessary();
        await foreach (var tableEntity in _tagsClient.QueryAsync<TableEntity>(e => e.PartitionKey == tag))
        {
            var item = await Get(Guid.Parse(tableEntity.RowKey));

            if (item != null)
                yield return item;
        }
    }

    public async Task<TodoItem?> Get(Guid id)
    {
        await InitializeIfNecessary();
        var response = await _todosClient.GetEntityIfExistsAsync<TodoTableEntity>(string.Empty, id.ToString("D"));

        if (response.HasValue)
        {
            response.Value!.UpdateTodoItemValuesFromEntity();
        }

        return response.HasValue ? response.Value : null;
    }

    public async Task Update(TodoItem todo)
    {
        await InitializeIfNecessary();
        var entity = TodoTableEntity.FromItem(todo);
        await _todosClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
    }

    public async Task Delete(Guid id)
    {
        await InitializeIfNecessary();

        var item = await Get(id);

        if (item == null)
            throw new InvalidOperationException("Item does not exist");

        await _todosClient.DeleteEntityAsync(string.Empty, GetId(item));

        foreach (string tag in item.Tags ?? Enumerable.Empty<string>())
        {
            await RemoveLookupEntity(_tagsClient, tag, item.Guid);
        }

        await RemoveLookupEntity(_topicsClient, item.Topic, item.Guid);
    }

    private async Task AddLookupEntity(TableClient client, string partitionKey, Guid todoId)
    {
        var tagEntity = new TableEntity();
        tagEntity.PartitionKey = partitionKey;
        tagEntity.RowKey = todoId.ToString("D");
        await client.AddEntityAsync(tagEntity);
    }

    private async Task RemoveLookupEntity(TableClient client, string partitionKey, Guid todoId) => await client.DeleteEntityAsync(partitionKey, todoId.ToString("D"));
   
    private string GetId(TodoItem item) => item.Guid.ToString("D");

    private Task InitializeIfNecessary() => _lazyInit.Value;

}