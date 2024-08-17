using Blazored.LocalStorage;

namespace BlazorGPT.Settings;

public class InterceptorConfigurationService(ILocalStorageService localStorageService)
{
    private const string StorageKey = "bgpt_interceptors";
    private readonly ILocalStorageService? _localStorageService = localStorageService;

    public async Task<IEnumerable<string>> GetConfig()
    {
        var model = await _localStorageService!.GetItemAsync<IEnumerable<string>>(StorageKey);
        return model;
    }

    public async Task SaveConfig(IEnumerable<string> config)
    {
        await _localStorageService!.SetItemAsync(StorageKey, config);
    }

    public async Task ResetConfig()
    {
        await _localStorageService!.RemoveItemAsync(StorageKey);
    }
}