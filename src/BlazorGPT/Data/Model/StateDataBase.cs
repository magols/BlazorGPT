namespace BlazorGPT.Data.Model;

public abstract class StateDataBase
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Content { get; set; }

    public string? Type { get; set; } = null!;
    public bool IsPublished { get; set; }
}