using System.Text.Json;
using Blazored.LocalStorage;
using BlazorGPT.Settings.PluginSelector;


namespace BlazorGPT.Settings;

public class PluginsConfigurationService(ILocalStorageService localStorageService)
{
    private const string StorageKey = "bgpt_plugins";

    public async Task<List<PluginSelection>?> GetConfig()
    {
        var data = await localStorageService.GetItemAsync<string>(StorageKey);
        if (data != null)
            return JsonSerializer.Deserialize<List<PluginSelection>>(data);

        return new List<PluginSelection>();
    }

    public async Task SaveConfig(IEnumerable<PluginSelection> config)
    {
        await localStorageService.SetItemAsync(StorageKey, config);
    }

    public async Task ResetConfig()
    {
        await localStorageService.RemoveItemAsync(StorageKey);
    }
}