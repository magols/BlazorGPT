using Azure;
using Blazored.LocalStorage;
using BlazorGPT.Settings;
using BlazorGPT.Settings.PluginSelector;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BlazorGPT.Pipeline.Interceptors;

public class FunctionCallingInterceptor : InterceptorBase, IInterceptor
{
    private CancellationToken _cancellationToken;
    private KernelService _kernelService;
    private readonly ILocalStorageService? _localStorageService;
    private readonly PluginsRepository _pluginsRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ModelConfigurationService _modelConfigurationService;

    public FunctionCallingInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kernelService = _serviceProvider.GetRequiredService<KernelService>();
        _localStorageService = _serviceProvider.GetRequiredService<ILocalStorageService>();
        _pluginsRepository = _serviceProvider.GetRequiredService<PluginsRepository>();
        _modelConfigurationService = _serviceProvider.GetRequiredService<ModelConfigurationService>();
    }

    public override string Name { get; } = "Function calling (select plugins)";


    public override async Task<Conversation> Send(Kernel kernel, Conversation conversation,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
  
        await Intercept(kernel, conversation, cancellationToken);
        conversation.StopRequested = true;
      
        return conversation;
    }
 

    private  async Task Intercept(Kernel kernel, Conversation conversation, CancellationToken cancellationToken)
    {
        var conversationState = _serviceProvider.GetRequiredService<CurrentConversationState>();
        conversationState.SetCurrentConversationForUser(conversation);

        var functionFilter = _serviceProvider.GetRequiredService<FunctionCallingFilter>();

        var approvalFilter = _serviceProvider.GetRequiredService<FunctionApprovalFilter>();

        var config = await _modelConfigurationService.GetConfig();
       
        kernel  = await _kernelService.CreateKernelAsync(config.Provider, config.Model, functionInvocationFilters: new List<IFunctionInvocationFilter>(){functionFilter, approvalFilter });

        await LoadPluginsAsync(kernel);

        conversation.AddMessage("assistant", "");
        OnUpdate?.Invoke();

        ChatHistory chatHistory = conversation.ToChatHistory();

        try
        {
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                TopP = 0,
                MaxTokens = _modelConfigurationService.GetDefaultConfig().MaxTokens,
                Temperature = 0
            };

            IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
       
            var response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel, cancellationToken: cancellationToken);

            conversation.Messages.Last().Content = response.Content;

        }
        catch (Exception e)
        {
            conversation.Messages.Last().Content = e.Message;
        }
        finally
        {
            OnUpdate?.Invoke();

            conversationState.RemoveCurrentConversation(conversation.UserId);
        }
    }

    private async Task LoadPluginsAsync(Kernel kernel)
    {
        var semanticPlugins =  _pluginsRepository.GetSemanticPlugins();

        List<Plugin> pluginsEnabledInSettings = new List<Plugin>();
        IEnumerable<string> enabledNames = Enumerable.Empty<string>();
        if (_localStorageService != null)
        {
            pluginsEnabledInSettings = 
                await _localStorageService.GetItemAsync<List<Plugin>>("bgpt_plugins", _cancellationToken);
            enabledNames = pluginsEnabledInSettings.Select(o => o.Name);
            semanticPlugins = semanticPlugins.Where(o => enabledNames.Contains(o.Name)).ToList();
        }

        foreach (var plugin in semanticPlugins)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", plugin.Name);
            kernel.ImportPluginFromPromptDirectory(path, plugin.Name);
        }


        var nativePlugins = new List<Plugin>();
        nativePlugins.AddRange(  _pluginsRepository.GetCoreNative());
        nativePlugins.AddRange(_pluginsRepository.GetExternalNative());
        nativePlugins.AddRange(_pluginsRepository.GetSemanticKernelPlugins());
        nativePlugins.AddRange( _pluginsRepository.GetKernelMemoryPlugins());
        var bing = _pluginsRepository.CreateBingPlugin();
        if (bing != null) nativePlugins.Add(bing);
        var google = _pluginsRepository.CreateGooglePlugin();
        if (google != null) nativePlugins.Add(google);

        nativePlugins = nativePlugins.Where(o => enabledNames.Contains(o.Name)).ToList();

        foreach (var plugin in nativePlugins)
        {

            try
            {
                string pluginName = plugin.Name.Substring(plugin.Name.LastIndexOf(".", StringComparison.Ordinal) + 1);
                kernel.ImportPluginFromObject(plugin.Instance, pluginName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Could not load native plugins", e);
            }
        }

        //// kernel memory plugin
        //var kernelMemoryPlugins = await _pluginsRepository.GetKernelMemoryPlugins();
        //kernelMemoryPlugins = kernelMemoryPlugins.Where(o => enabledNames.Contains(o.Name)).ToList();

        //foreach (var plugin in kernelMemoryPlugins)
        //{
        //    try
        //    {
        //        string pluginName = plugin.Name.Substring(plugin.Name.LastIndexOf(".", StringComparison.Ordinal) + 1);
        //        kernel.ImportPluginFromObject(plugin.Instance, pluginName);
        //    }
        //    catch (Exception e)
        //    {
        //        throw new InvalidOperationException("Could not load kernel memory plugins", e);
        //    }
        //}
    }
}