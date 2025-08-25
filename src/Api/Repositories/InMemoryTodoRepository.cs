using System.Collections.Concurrent;
using Api.Models;

namespace Api.Repositories;

public sealed class InMemoryTodoRepository : ITodoRepository
{
    private readonly ConcurrentDictionary<Guid, TodoItem> _store = new();

    public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TodoItem>>(_store.Values.OrderBy(x => x.CreatedAt).ToList());

    public Task<TodoItem?> GetAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_store.TryGetValue(id, out var v) ? v : null);

    public Task<TodoItem> CreateAsync(TodoItem item, CancellationToken ct)
    {
        item.CreatedAt = DateTimeOffset.UtcNow;
        item.UpdatedAt = item.CreatedAt;
        _store[item.Id] = item;
        return Task.FromResult(item);
    }

    public Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken ct)
    {
        if (!_store.ContainsKey(item.Id)) return Task.FromResult<TodoItem?>(null);
        item.UpdatedAt = DateTimeOffset.UtcNow;
        _store[item.Id] = item;
        return Task.FromResult<TodoItem?>(item);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_store.TryRemove(id, out _));
}
