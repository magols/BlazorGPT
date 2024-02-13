using Blazored.LocalStorage;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;

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
            Provider = _pipelineOptions.Providers.GetChatModelsProvider(),
            Model = _pipelineOptions.Providers.GetChatModel(),
            MaxTokens = _pipelineOptions.MaxTokens,
            Temperature = 0.0f,
            EmbeddingsModel = _pipelineOptions.Providers.GetEmbeddingsModel(),
            EmbeddingsProvider = _pipelineOptions.Providers.GetEmbeddingsModelProvider()
        };
    }

    public async Task<ModelConfiguration> GetConfig()
    {
        if (_userConfig != null) 
            return _userConfig;

        ModelConfiguration? model = null;
        try
        {
            model = await _localStorageService!.GetItemAsync<ModelConfiguration?>(StorageKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
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