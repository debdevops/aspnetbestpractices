using System.ComponentModel.DataAnnotations;

namespace Api.Dtos;

public sealed class CreateTodoDto
{
    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed class UpdateTodoDto
{
    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsComplete { get; set; }
}
