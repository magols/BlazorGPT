using System.Security.Claims;
using BlazorGPT.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

namespace BlazorGPT.Settings.PluginSelector;

public abstract class PluginTogglerBase : ComponentBase
{
    [CascadingParameter]
    public required Task<AuthenticationState> AuthenticationState { get; set; }

    private string UserId;


    [Inject]
    public required PluginsConfigurationService PluginsConfigurationService { get; set; }

    [Inject]
    public required InterceptorConfigurationService InterceptorConfigurationService { get; set; }

    [Inject]
    public required SettingsStateNotificationService SettingsState { get; set; }

    [Parameter]
    public Action<bool>? OnToggle { get; set; }

    protected bool Enabled;

    protected virtual List<string> InterceptorNames { get; } = new();
    protected virtual List<string> PluginNames { get; } = new();


    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationState;
        var user = authState?.User;
        if (user?.Identity is not null && user.Identity.IsAuthenticated)
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var interceptorConfig = (await InterceptorConfigurationService.GetConfig()).ToList();
            var pluginsConfig = await PluginsConfigurationService.GetConfig();

            Enabled = InterceptorNames.All(name => interceptorConfig.Contains(name)) &&
                      PluginNames.All(name => pluginsConfig!.Any(plugin => plugin.Name == name));
            StateHasChanged();
        }
    }

    protected async Task Toggle()
    {
        Enabled = !Enabled;

        StateHasChanged();
        var interceptorConfig = (await InterceptorConfigurationService.GetConfig()).ToList();
        var pluginsConfig = await PluginsConfigurationService.GetConfig();

        if (Enabled)
        {
            foreach (var name in InterceptorNames)
            {
                if (interceptorConfig.All(x => x != name))
                {
                    interceptorConfig.Add(name);
                }
            }

            foreach (var name in PluginNames)
            {
                if (pluginsConfig.All(x => x.Name != name))
                {
                    pluginsConfig!.Add(new PluginSelection()
                    { Name = name, Selected = true });
                }
            }
        }
        else
        {
            foreach (var name in InterceptorNames)
            {
                if (interceptorConfig.Any(x => x == name))
                {
                    interceptorConfig.Remove(name);
                }
            }

            foreach (var name in PluginNames)
            {
                if (pluginsConfig!.Any(x => x.Name == name))
                {
                    pluginsConfig!.Remove(pluginsConfig!.First(x => x.Name == name));
                }
            }
        }

        await InterceptorConfigurationService.SaveConfig(interceptorConfig);
        await PluginsConfigurationService.SaveConfig(pluginsConfig!);


        StateHasChanged();

        if (OnToggle != null)
        {
            OnToggle.Invoke(Enabled);
        }

        await SettingsState.NotifySettingsChanged(new SettingsChangedNotification() { UserId = UserId, Type = typeof(PluginTogglerBase) });
    }
}
