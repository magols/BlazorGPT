using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace BlazorGPT.Shared.PluginSelector;

public partial class PluginsList
{
    private readonly PluginFormModel _model = new();


    private List<Plugin> _plugins = new();

    [Inject] 
    private ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    PluginsRepository? PluginsRepository { get; set; }
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
            _model.Plugins.Add(new PluginSelection { Name = plugin.Name });
            _model.SelectedPlugins.Add(new PluginSelection { Name = plugin.Name });
        }
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
}



