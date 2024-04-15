using Microsoft.AspNetCore.Mvc;
using TelemetryTest.Common.Data;
using TelemetryTest.Common.Model;

namespace TelemetryTest.WebApi.Controllers;

public class TodosController : ControllerBase
{
    private readonly ITodoRepository _repository;

    public TodosController(ITodoRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("/todos")]
    public async Task<IActionResult> AddTodo([FromBody] TodoItem todo)
    {
        await _repository.AddTodo(todo);
        return Created();
    }

    [HttpGet("/todos")]
    public async Task<IActionResult> GetLatestTodos([FromQuery] int count)
    {
        var todos = await _repository.GetLatestTodos(count);
        return Ok(todos);
    }

    [HttpDelete("/todos/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _repository.Delete(id);
        return NoContent();
    }
}