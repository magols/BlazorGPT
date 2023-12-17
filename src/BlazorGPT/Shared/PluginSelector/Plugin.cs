namespace BlazorGPT.Shared.PluginSelector;

public class Plugin
{
    public string Name { get; set; } = string.Empty;
    public ICollection<Function> Functions { get; set; } = new List<Function>();
}