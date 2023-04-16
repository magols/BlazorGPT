namespace BlazorGPT.Data;

public class Script
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; }

    public string Name { get; set; }

    public string SystemMessage { get; set; } = "You are a helpful assistant";

    public List<ScriptStep> Steps { get; set; } = new();
}