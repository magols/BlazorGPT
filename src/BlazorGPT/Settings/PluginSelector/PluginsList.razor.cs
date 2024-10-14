using BlazorGPT.Components.Memories;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace BlazorGPT.Settings.PluginSelector;

public partial class PluginsList
{
 
    private List<Plugin> _plugins = new();
    private RadzenDataGrid<Plugin> _grid;
    private IList<Plugin> _selPlugins;
    List<PluginSelection>? BrowserData { get; set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }
    public string UserId { get; set; } = null!;

    [Inject]
    public required SettingsStateNotificationService SettingsStateNotificationService { get; set; }

    [Inject] 
    public required PluginsConfigurationService PluginsConfigurationService { get; set; }

    [Inject]
    public required PluginsRepository PluginsRepository { get; set; }

    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetSelectionsFromLocalStorage();

            if (BrowserData != null)
            {
                foreach (var plugin in BrowserData)
                {
                    var e = _plugins.FirstOrDefault(o => o.Name == plugin.Name);
                    if (e != null)
                    {
                       await _grid.SelectRow(e, true);
                    }
                }
            }

            await _grid.Reload();
            _loaded = true;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            var user = authState?.User;
            if (user?.Identity is not null && user.Identity.IsAuthenticated)
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            }
        }

        _selPlugins = new List<Plugin>();
        var all = await PluginsRepository.All();
        foreach (var plugin in all)
        {
            _plugins.Add(plugin);
        }
        StateHasChanged();

        SettingsStateNotificationService.OnUpdate += SettingsChanged;
    }

    private async Task SettingsChanged(SettingsChangedNotification obj)
    {
        if (obj.UserId == UserId && obj.Type == typeof(PluginsList))
        {
            _plugins.Clear();
            var all = await PluginsRepository.All();
            foreach (var plugin in all)
            {
                _plugins.Add(plugin);
            }
            StateHasChanged();
        }
    }


    private async Task GetSelectionsFromLocalStorage()
    {
        BrowserData = await PluginsConfigurationService.GetConfig();
    }
    
    private async Task Submit()
    {
        var pluginSelections = _selPlugins.Select( o => new PluginSelection() {Name = o.Name, Selected = true} );
        await PluginsConfigurationService.SaveConfig(pluginSelections);

          SettingsStateNotificationService.NotifySettingsChanged(new SettingsChangedNotification()
          {
              UserId = UserId,
              Type = typeof(PluginsList)
          });


    }

    private async Task ClearAll()
    {
        _selPlugins.Clear();
        _grid.Reset(false);
        await Submit();
        StateHasChanged();

    }

  

    private async Task RowSelect(Plugin arg)
    {
        if (_loaded)
        {
            _selPlugins.Add(arg);
            await Submit();
        }
    }

    private async Task RowDeselect(Plugin arg)
    {
       _selPlugins.Remove(arg);
        await Submit();
    }
}
