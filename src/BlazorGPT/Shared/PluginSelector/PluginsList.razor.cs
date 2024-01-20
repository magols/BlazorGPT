using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using Radzen;

namespace BlazorGPT.Shared.PluginSelector;

public partial class PluginsList
{
    private readonly PluginFormModel _model = new();
    private readonly PluginFormModel _modelOrig = new();


    private List<Plugin> _plugins = new();
    private DirectoryInfo? _pluginsDirectory;

    [Inject] 
    private ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    PluginsRepository PluginsRepository { get; set; }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetSelectionsFromLocalStorage();

            if (BrowserData != null)
            {
                foreach (var plugin in BrowserData)
                {
                   var exists = _model.SelectedPlugins.FirstOrDefault(o => o.Name == plugin.Name);
                   if (exists != null)
                   {
                          exists.Selected = plugin.Selected;
                   }
                }
                StateHasChanged();
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
     
        foreach (var plugin in await PluginsRepository.All())
        {
            _plugins.Add(plugin);
            _model.SelectedPlugins.Add(new PluginSelection { Name = plugin.Name });
            _modelOrig.OriginalPlugins.Add(new PluginSelection { Name = plugin.Name });
        }

        //var nativePlugins = FindTypesWithKernelFunctionAttribute();
        //foreach (var (type, method, desc) in nativePlugins)
        //{
        //    var plugin = new Plugin
        //    {
        //        Name = type,
        //        Functions = new List<Function>()
        //    };
        //    var f = new Function
        //    {
        //        Name = method,
        //        Description = desc
        //    };
        //    plugin.Functions.Add(f);
        //    _plugins.Add(plugin);
        //    _model.SelectedPlugins.Add(new PluginSelection { Name = plugin.Name });
        //    _modelOrig.OriginalPlugins.Add(new PluginSelection { Name = plugin.Name });
        //}

    }



    private async Task GetSelectionsFromLocalStorage()
    {
        if (LocalStorage != null)
        {
            var data = await LocalStorage.GetItemAsync<string>("bgpt_plugins");
            if (data != null)
              BrowserData =  JsonSerializer.Deserialize<List<PluginSelection>>(data);
        }
    }

     List<PluginSelection>? BrowserData { get; set; }

    private async Task SetSelectionsInLocalStorage()
    {
        if (LocalStorage != null)
        {
            await LocalStorage.SetItemAsync("bgpt_plugins", _model.SelectedPlugins.Where(o => o.Selected));
        }
    }

    private async Task Submit()
    {
        if (LocalStorage != null)
        {
            await SetSelectionsInLocalStorage();
        }
    }

    private List<(string, string, string)> FindTypesWithKernelFunctionAttribute()
    {
        Assembly assembly = Assembly.GetExecutingAssembly(); // Replace with the desired assembly

        var typesWithKernelFunctionAttribute = assembly.GetTypes()
            .Where(type => type.GetMethods()
                .Any(method => method.GetCustomAttributes(typeof(KernelFunctionAttribute), true).Any()))
            .ToList();
        List<(string, string, string)> types = new List<(string, string, string)>();
        foreach (var type in typesWithKernelFunctionAttribute)
        {
            foreach (var method in type.GetMethods())
            {
                var attr = method.GetCustomAttribute<KernelFunctionAttribute>();
                if (attr != null)
                {
                    var descAttr = method.GetCustomAttribute<DescriptionAttribute>();
                    string desc = descAttr?.Description ?? "";
                    // desc should be set to the DescriptionAttribute of the method

                    types.Add((type.FullName, method.Name, desc));
                }
            }
        }

        return types;
    }
}



public class SemanticPluginConfig
{
    public int? schema { get; set; }
    public string type { get; set; }
    public string description { get; set; }
    public CompletionConfig completion { get; set; }
    public InputConfig input { get; set; }
    public object[] default_backends { get; set; }
}

public class CompletionConfig
{
    public int? max_tokens { get; set; }
    public decimal temperature { get; set; }
    public decimal top_p { get; set; }
    public decimal presence_penalty { get; set; }
    public decimal frequency_penalty { get; set; }
}

public class InputConfig
{
    public ParameterConfig[] parameters { get; set; }
}

public class ParameterConfig
{
    public string name { get; set; }
    public string description { get; set; }
    public string defaultValue { get; set; }
}
