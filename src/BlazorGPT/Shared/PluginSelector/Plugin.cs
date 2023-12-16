namespace BlazorGPT.Shared.PluginSelector;

internal class Plugin
{
    public string? Name { get; set; }
    public ICollection<Function> Functions { get; set; } = new List<Function>();
}