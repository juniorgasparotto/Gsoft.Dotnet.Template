namespace ToDoSystem.Core.Entities;

/// <summary>
/// Entity to represent a ToDo list item.
/// </summary>
public class TodoItem
{
    public int Id { get; set; }

    /// <summary>
    /// Item title/task.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    public TypeEnum Teste { get; set; }
    public TestEnum Teste2 { get; set; }

    /// <summary>
    /// Optional item description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates if the item is completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Item creation date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update date and time of the item.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
