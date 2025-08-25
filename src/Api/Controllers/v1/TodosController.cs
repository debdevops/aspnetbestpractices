using Api.Dtos;
using Api.Models;
using Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Api.Extensions;
using Asp.Versioning;

namespace Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/todos")]
[ApiVersion("1.0")]
public sealed class TodosController : ControllerBase
{
    private readonly ITodoRepository _repo;
    private readonly ILogger<TodosController> _logger;

    public TodosController(ITodoRepository repo, ILogger<TodosController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAll(CancellationToken ct)
        => Ok(await _repo.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _repo.GetAsync(id, ct);
        if (item is null) return NotFound();
        Response.Headers.ETag = item.ETag;
        return Ok(item);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.ExpensivePolicy)]
    public async Task<IActionResult> Create([FromBody] CreateTodoDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var entity = new TodoItem { Title = dto.Title.Trim(), Notes = dto.Notes };
        var created = await _repo.CreateAsync(entity, ct);
        Response.Headers.ETag = created.ETag;
        _logger.LogInformation("Created Todo {Id}", created.Id);
        return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.ExpensivePolicy)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var existing = await _repo.GetAsync(id, ct);
        if (existing is null) return NotFound();

        if (Request.Headers.TryGetValue("If-Match", out var ifMatch))
        {
            var provided = ifMatch.ToString();
            if (!string.Equals(provided, existing.ETag, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status412PreconditionFailed, new
                {
                    type = "https://httpstatuses.io/412",
                    title = "Precondition Failed",
                    status = 412,
                    detail = "ETag does not match the current resource state."
                });
            }
        }

        existing.Title = dto.Title.Trim();
        existing.Notes = dto.Notes;
        existing.IsComplete = dto.IsComplete;

        var updated = await _repo.UpdateAsync(existing, ct);
        if (updated is null) return NotFound();

        Response.Headers.ETag = updated.ETag;
        _logger.LogInformation("Updated Todo {Id}", id);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [EnableRateLimiting(RateLimitingExtensions.ExpensivePolicy)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var ok = await _repo.DeleteAsync(id, ct);
        if (!ok) return NotFound();
        _logger.LogInformation("Deleted Todo {Id}", id);
        return NoContent();
    }
}
