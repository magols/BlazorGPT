namespace BlazorGPT.Data.Model;

public class ScriptStep
{
    public Guid Id { get; set; }
    public string Message { get; set; } = default!;
    public string Role { get; set; } = default!;
    public int SortOrder { get; set; }
    public Script? Script { get; set; } = default!;
    public Guid ScriptId { get; set; }
}