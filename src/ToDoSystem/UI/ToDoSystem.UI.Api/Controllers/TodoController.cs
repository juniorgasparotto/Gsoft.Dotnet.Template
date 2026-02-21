using Microsoft.AspNetCore.Mvc;
using Shared.Core.Repositories;
using ToDoSystem.Core.Entities;
using ToDoSystem.Infra;
using ToDoSystem.Infra.Repositories.EfCore;

namespace ToDoSystem.UI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodoController : ControllerBase
{
    private readonly IRepository<TodoItem> _repository;
    private readonly DefaultDbContext _dbContext;

    public TodoController(IRepository<TodoItem> repository, DefaultDbContext dbContext)
    {
        _repository = repository;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll(CancellationToken cancellationToken = default)
    {
        var options = new RepositoryOptions { AsNoTracking = true };
        var query = _repository.GetAll(options);
        var items = await _repository.ToIEnumerableAsync(query, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoItem>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var options = new RepositoryOptions { AsNoTracking = true };
        var item = await _repository.GetByIdAsync(options, id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> Create(
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        _repository.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TypeEnum>> Update(Guid id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var options = new RepositoryOptions();
        var item = await _repository.GetByIdAsync(options, id);
        if (item is null)
            return NotFound();
        item.Title = request.Title;
        item.Description = request.Description;
        item.IsCompleted = request.IsCompleted;
        item.UpdatedAt = DateTime.UtcNow;
        _repository.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<ActionResult<TypeEnum>> SetComplete(Guid id, [FromBody] SetCompleteRequest request, CancellationToken cancellationToken = default)
    {
        var options = new RepositoryOptions();
        var item = await _repository.GetByIdAsync(options, id);
        if (item is null)
            return NotFound();
        item.IsCompleted = request.IsCompleted;
        item.UpdatedAt = DateTime.UtcNow;
        _repository.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var options = new RepositoryOptions();
        var item = await _repository.GetByIdAsync(options, id);
        if (item is null)
            return NotFound();
        _repository.Delete(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record CreateTodoRequest(string Title, string? Description = null);

public record UpdateTodoRequest(string Title, string? Description, bool IsCompleted);

public record SetCompleteRequest(bool IsCompleted);