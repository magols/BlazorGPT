using Blazored.LocalStorage;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.Options;

namespace BlazorGPT.Settings;

public class ModelConfigurationService
{
    private const string StorageKey = "bgpt_model";
    readonly ILocalStorageService? _localStorageService;
    readonly PipelineOptions _pipelineOptions;
   
    ModelConfiguration? _userConfig { get; set; }
    
    public ModelConfigurationService(ILocalStorageService localStorageService, IOptions<PipelineOptions>? pipelineOptions)
    {
        _localStorageService = localStorageService;
        _pipelineOptions = pipelineOptions?.Value ?? throw new ArgumentNullException(nameof(pipelineOptions));
    }

    public ModelConfiguration GetDefaultConfig()
    {
        return new ModelConfiguration()
        {
            Model = _pipelineOptions.Model,
            MaxTokens = _pipelineOptions.MaxTokens,
            Temperature = 0.0f
        };
    }

    public async Task<ModelConfiguration> GetConfig()
    {
        if (_userConfig != null) 
            return _userConfig;

        var model = await _localStorageService!.GetItemAsync<ModelConfiguration?>(StorageKey);
        if (model == null)
        {
            model = GetDefaultConfig();
            await SaveConfig(model);
        }
 
        return model;
    }

    public async Task SaveConfig(ModelConfiguration model)
    {
        _userConfig = model;
        await _localStorageService!.SetItemAsync(StorageKey, _userConfig);
    }

    public async Task Reset()
    {
        _userConfig = null;
        await _localStorageService!.RemoveItemAsync(StorageKey);
    }

}