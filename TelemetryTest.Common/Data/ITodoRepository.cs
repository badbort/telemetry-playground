using TelemetryTest.Common.Model;

namespace TelemetryTest.Common.Data;

public interface ITodoRepository
{
    Task<Guid> AddTodo(TodoItem todo);
    
    Task<bool> Exists(Guid id);
    
    Task<List<TodoItem>> GetLatestTodos(int count);

    Task<TodoItem?> Get(Guid id);

    Task Update(TodoItem todo);

    Task Delete(Guid id);

    Task<List<string>> GetTags();

    Task<List<string>> GetTopics();

    Task<List<TodoItem>> FromTopic(string topic, int count);
    
    Task<List<TodoItem>> FromTag(string tag, int count);
}