using Api.Models;

namespace Api.Repositories;

public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct);
    Task<TodoItem?> GetAsync(Guid id, CancellationToken ct);
    Task<TodoItem> CreateAsync(TodoItem item, CancellationToken ct);
    Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
