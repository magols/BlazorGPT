using System.Text.Json;

namespace BlazorGPT.Shared.PluginSelector
{
    public class PluginsRepository
    {

        public async Task <List<Plugin>> GetFromDiskAsync()
        {
            var pluginsDirectory = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Plugins"));

            var plugins = new List<Plugin>();

            foreach (var pluginDirectory in pluginsDirectory.EnumerateDirectories())
            {
                var plugin = new Plugin
                {
                    Name = pluginDirectory.Name,
                    Functions = new List<Function>()
                };
                // iterate the plugin directories to get the functions and their descriptions
                foreach (var pluginFile in pluginDirectory.EnumerateDirectories())
                {
                    var configString = await File.ReadAllTextAsync(Path.Join(pluginFile.FullName, "config.json"));

                    SemanticPluginConfig?
                        config = JsonSerializer.Deserialize<SemanticPluginConfig>(configString);

                    var f = new Function
                    {
                        Name = pluginFile.Name,
                        Description = @config?.description

                    };
                    plugin.Functions.Add(f);
                }

                plugins.Add(plugin);
            }

            return plugins;
        }
    }
}
