namespace BlazorGPT.Shared.PluginSelector;

public class Plugin
{
    public bool IsNative;
    public string Name { get; set; } = string.Empty;
    public ICollection<Function> Functions { get; set; } = new List<Function>();

    public object? Instance { get; set; }
}   