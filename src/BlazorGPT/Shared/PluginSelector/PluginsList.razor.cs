using BlazorGPT.Settings;
using Microsoft.AspNetCore.Components;

namespace BlazorGPT.Shared.PluginSelector;

public partial class PluginsList
{
    private readonly PluginFormModel _model = new();
    private List<Plugin> _plugins = new();
    List<PluginSelection>? BrowserData { get; set; }

    [Inject] 
    public required PluginsConfigurationService PluginsConfigurationService { get; set; }

    [Inject]
    public required PluginsRepository PluginsRepository { get; set; }

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
        BrowserData = await PluginsConfigurationService.GetConfig();
    }
    
    private async Task Submit()
    {

        await PluginsConfigurationService.SaveConfig(_model.SelectedPlugins.Where(o => o.Selected));

    }
}
