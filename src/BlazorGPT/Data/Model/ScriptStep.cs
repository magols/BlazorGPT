namespace BlazorGPT.Data.Model;

public class ScriptStep
{
    public Guid Id { get; set; }
    public string Message { get; set; }
    public string Role { get; set; }
    public int SortOrder { get; set; }
    public Script Script { get; set; }
    public Guid ScriptId { get; set; }
}