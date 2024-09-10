using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web.Google;

#pragma warning disable SKEXP0050

namespace BlazorGPT.Settings.PluginSelector;

public class PluginsRepository
{
    private IServiceProvider _serviceProvider;
    private readonly string _pluginsDirectory;
    private PipelineOptions _options;

    public PluginsRepository(IServiceProvider serviceProvider, string pluginsDirectory)
    {
        _serviceProvider = serviceProvider;
        _pluginsDirectory = pluginsDirectory;
        _options = _serviceProvider.GetRequiredService<IOptions<PipelineOptions>>().Value;
    }

    public PluginsRepository(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        _options = _serviceProvider.GetRequiredService<IOptions<PipelineOptions>>().Value;
    }

    public async Task<List<Plugin>> All()
    {
        var coreNative = GetCoreNative();
        var semanticPlugins = GetSemanticPlugins();
        var externalNative = GetExternalNative();
        var kernelMemory = GetKernelMemoryPlugins();
        var semanticKernelCore =  GetSemanticKernelPlugins();
        var plugins = new List<Plugin>();
        plugins.AddRange(coreNative);
        plugins.AddRange(externalNative);
        plugins.AddRange(semanticPlugins);
        plugins.AddRange(kernelMemory);
        plugins.AddRange(semanticKernelCore);

        var bing = CreateBingPlugin();
        if (bing != null) plugins.Add(bing);

        var google = CreateGooglePlugin();
        if (google != null) plugins.Add(google);

        return plugins.OrderBy(o => o.Name).ToList();
    }

    public Plugin? CreateBingPlugin()
    {
        if (_options.BING_API_KEY == null || _options.BING_API_KEY == "[get a key]")
        {
            return null;
        }

        var bing = new BingConnector(_options.BING_API_KEY);
        var web = new WebSearchEnginePlugin(bing);
        Plugin p = new Plugin
        {
            Name = "Bing",
            IsNative = true,
            Instance = web
        };

        var methods = typeof(WebSearchEnginePlugin).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any())
            .ToList();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
            if (attr != null)
            {
                var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                string desc = descAttr?.Description ?? "";
                var f = new Function
                {
                    Name = method.Name,
                    Description = desc
                };
                p.Functions.Add(f);
            }
        }

        return p;
    }


    public Plugin? CreateGooglePlugin()
    {
        if (_options.GOOGLE_API_KEY == null || _options.GOOGLE_SEARCH_ENGINE_ID == null || _options.GOOGLE_API_KEY== "[get a key]")
        {
            return null;
        }

        var bing = new GoogleConnector(_options.GOOGLE_API_KEY, _options.GOOGLE_SEARCH_ENGINE_ID);
        var web = new WebSearchEnginePlugin(bing);
        Plugin p = new Plugin
        {
            Name = "Google",
            IsNative = true,
            Instance = web
        };

        var methods = typeof(WebSearchEnginePlugin).GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any())
            .ToList();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
            if (attr != null)
            {
                var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                string desc = descAttr?.Description ?? "";
                var f = new Function
                {
                    Name = method.Name,
                    Description = desc
                };
                p.Functions.Add(f);
            }
        }

        return p;
    }

    public  async Task<List<Plugin>> GetCore()
    {
        var internalNative = GetCoreNative();
        var semanticPlugins =  GetSemanticPlugins();

        var plugins = new List<Plugin>();
        plugins.AddRange(internalNative);
        plugins.AddRange(semanticPlugins);
        return plugins;
    }

    private IEnumerable<Type> GetTypesWithKernelFunctionAttribute(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type.GetMethods()
                .Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()));
    }

    public List<Plugin> GetCoreNative()
    {
        var plugins = new List<Plugin>();
        {
            var types = GetTypesWithKernelFunctionAttribute(Assembly.GetAssembly(typeof(Plugin))!);

            foreach (var type in types)
            {
                var instance = TryCreatePluginInstance(type, _serviceProvider);

                if (instance != null)
                {
                    plugins.Add(instance);
                }
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
            var types = GetTypesWithKernelFunctionAttribute(Assembly.LoadFile(file));

            foreach (var type in types)
            {

                var p = TryCreatePluginInstance(type, _serviceProvider);
                if (p != null)
                {
                    plugins.Add(p);
                }
            }
        }

        return plugins;
    }

    public List<Plugin> GetSemanticPlugins()
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
                var configString = File.ReadAllText(Path.Join(pluginFile.FullName, "config.json"));

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
    public List<Plugin> GetKernelMemoryPlugins()
    {

        var options = _serviceProvider.GetRequiredService<IOptions<PipelineOptions>>().Value;

        var plugins = new List<Plugin>();
        var kmPlugin = new MemoryPlugin(options.Memory.Url,  options.Memory.ApiKey);
            
        var plugin = new Plugin
        {
            Name = "Kernel Memory",
            Functions = new List<Function>()
        };

             Assembly assembly = Assembly.GetAssembly(typeof(MemoryPlugin))!;
        var kmType = assembly.GetTypes()
            .First(type => type == typeof(MemoryPlugin) && type.GetMethods().Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()));

        // TODO: till function
        var instance = Activator.CreateInstance(kmType, options.Memory.Url, options.Memory.ApiKey, false);
        
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


    // load all plugins from Microsoft.SemanticKernel.Plugins assembly
    public List<Plugin> GetSemanticKernelPlugins()
    {
        var plugins = new List<Plugin>();

        var types = GetTypesWithKernelFunctionAttribute(Assembly.GetAssembly(typeof(TimePlugin))!);

        foreach (var type in types)
        {
            var instance = TryCreatePluginInstance(type);
            if (instance != null)
            {
                plugins.Add(instance);
            }
        }

        return plugins;
    }
 
    public Plugin? TryCreatePluginInstance(Type type, params object[] args)
    {
        try
        {
            var instance = Activator.CreateInstance(type, args: args);
            Plugin p = new Plugin
            {
                Name = type.ToString(),
                IsNative = true,
                Instance = instance
            };


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
                    var f = new Function
                    {
                        Name = method.Name,
                        Description = desc
                    };
                    p.Functions.Add(f);
                }
            }

            return p;

        }

        catch (Exception e)
        {
            return null;
        }

    }
}
