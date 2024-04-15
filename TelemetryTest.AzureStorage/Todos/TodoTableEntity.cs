using Azure;
using Azure.Data.Tables;
using MoreLinq.Extensions;
using TelemetryTest.Common.Model;

namespace TelemetryTest.AzureStorage.Todos;

internal class TodoTableEntity : TodoItem, ITableEntity
{
    public string? PartitionKey { get; set; }
    
    public string? RowKey { get; set; }
    
    public DateTimeOffset? Timestamp { get; set; }
    
    public ETag ETag { get; set; }
    
    /// <summary>
    /// Comma separated tags from <see cref="TodoItem.Tags"/>
    /// </summary>
    public string TagsList { get; set; }

    public void UpdateTableEntityValues(TodoItem? inner = null)
    {
        inner ??= this;
        Guid = inner.Guid;
        Message = inner.Message;
        Topic = inner.Topic;
        Hidden = inner.Hidden;
        Created = inner.Created;
        LastModified = inner.LastModified;
        Created = inner.Created;
        TagsList = inner.Tags?.ToDelimitedString(",") ?? string.Empty;
        (PartitionKey, RowKey) = GetKeys(inner);
    }

    public void UpdateTodoItemValuesFromEntity()
    {
        Tags = string.IsNullOrEmpty(TagsList) ? null : TagsList.Split(",").ToList();
    }

    public static (string PartitionKey, string RowKey) GetKeys(TodoItem item) => (string.Empty, item.Guid.ToString("D"));

    public static TodoTableEntity FromItem(TodoItem item)
    {
        var entity = new TodoTableEntity();
        entity.UpdateTableEntityValues(item);
        return entity;
    }
}