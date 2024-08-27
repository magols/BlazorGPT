using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BlazorGPT.Migrations;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Settings.PluginSelector
{
    public class PluginsRepository
    {
        private IServiceProvider _serviceProvider;
        private readonly string _pluginsDirectory;

        public PluginsRepository(IServiceProvider serviceProvider, string pluginsDirectory)
        {
            _serviceProvider = serviceProvider;
            _pluginsDirectory = pluginsDirectory;
        }

        public PluginsRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

        }

        public async Task<List<Plugin>> All()
        {
            var coreNative = GetCoreNative();
            var semanticPlugins = await GetSemanticPlugins();
            var externalNative = GetExternalNative();
            var kernelMemory = await GetKernelMemoryPlugins();

            var plugins = new List<Plugin>();
            plugins.AddRange(coreNative);
            plugins.AddRange(externalNative);
            plugins.AddRange(semanticPlugins);
            plugins.AddRange(kernelMemory);

            return plugins.OrderBy(o => o.Name).ToList();
        }

        public  async Task<List<Plugin>> GetCore()
        {
            var internalNative = GetCoreNative();
            var semanticPlugins =  await  GetSemanticPlugins();

            var plugins = new List<Plugin>();
        
            plugins.AddRange(internalNative);

            plugins.AddRange(semanticPlugins);
            return plugins;
        }

        public List<Plugin> GetCoreNative()
        {
            var plugins = new List<Plugin>();
            {
                Assembly assembly = Assembly.GetAssembly(typeof(Plugin))!;
                var types = assembly.GetTypes()
                    .Where(type => type.GetMethods()
                                           .Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()))
                    .ToList();


                foreach (var type in types)
                {
                    var instance = Activator.CreateInstance(type, _serviceProvider);
                    Plugin p = new Plugin() { Name = type.ToString(), IsNative = true };
                    p.Instance = instance;


                    var methods = type.GetMethods()
                        .Where(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any())
                        .ToList();

                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
                        if (attr != null)
                        {
                            var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                            string desc = descAttr?.Description ?? "";
                            // desc should be set to the DescriptionAttribute of the method

                            var f = new Function
                            {
                                Name = method.Name,
                                Description = desc
                            };
                            p.Functions.Add(f);
                        }
                    }

                    plugins.Add(p);
                }
            }

            return plugins;
        }

        public List<Plugin> GetExternalNative()
        {
            var plugins = new List<Plugin>();
            var files = Directory.GetFiles(_pluginsDirectory, "*.dll");

            foreach (var file in files)
            {
                Assembly assembly = Assembly.LoadFile(file);
                var types = assembly.GetTypes()
                    .Where(type => type.GetMethods()
                                           .Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()))
                    .ToList();


                foreach (var type in types)
                {
                    var instance = Activator.CreateInstance(type, _serviceProvider);
                    Plugin p = new Plugin() { Name = type.ToString(), IsNative = true };
                    p.Instance = instance;


                    var methods = type.GetMethods()
                        .Where(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any())
                        .ToList();

                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
                        if (attr != null)
                        {
                            var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                            string desc = descAttr?.Description ?? "";
                            // desc should be set to the DescriptionAttribute of the method

                            var f = new Function
                            {
                                Name = method.Name,
                                Description = desc
                            };
                            p.Functions.Add(f);
                        }
                    }

                    plugins.Add(p);
                }
            }

            return plugins;
        }

        public async Task <List<Plugin>> GetSemanticPlugins()
        {

            var plugins = new List<Plugin>();
            var pluginsDirectory = new DirectoryInfo(_pluginsDirectory);

            foreach (var pluginDirectory in pluginsDirectory.EnumerateDirectories())
            {
                var plugin = new Plugin
                {
                    Name = pluginDirectory.Name,
                    Functions = new List<Function>()
                };

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

        public async Task<List<Plugin>> GetByNames(IEnumerable<string> pluginsNames)
        {
            var allPlugins = await All();
            return allPlugins.Where(p => pluginsNames.Contains(p.Name)).ToList();

        }


        // get kernel memory plugins
        public async Task<List<Plugin>> GetKernelMemoryPlugins()
        {
            var plugins = new List<Plugin>();

            var kmPlugin = new MemoryPlugin("http://localhost:9001", "api", false);
            
            var plugin = new Plugin
            {
                Name = "Kernel Memory",
                Functions = new List<Function>()
            };

            Assembly assembly = Assembly.GetAssembly(typeof(MemoryPlugin))!;
            var kmType = assembly.GetTypes()
                .First(type => type == typeof(MemoryPlugin) && type.GetMethods().Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()));

          
                var instance = Activator.CreateInstance(kmType, "http://localhost:9001", "api", false);
                Plugin p = new Plugin() { Name = kmType.ToString(), IsNative = true };
                p.Instance = instance;


                var methods = kmType.GetMethods()
                    .Where(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any())
                    .ToList();

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
                    if (attr != null)
                    {
                        var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                        string desc = descAttr?.Description ?? "";
                        // desc should be set to the DescriptionAttribute of the method

                        var f = new Function
                        {
                            Name = method.Name,
                            Description = desc
                        };
                        p.Functions.Add(f);
                    }
                }

                plugins.Add(p);
            

            return plugins;
        }
    }
}
