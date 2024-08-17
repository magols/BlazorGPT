using Blazored.LocalStorage;
using BlazorGPT.Settings;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BlazorGPT.Pipeline.Interceptors;



public class FunctionCallingFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Console.WriteLine(context.Function.Name);

        await next(context);

        // Get function invocation result
        var result = context.Result;

        Console.WriteLine(result);
    }
}


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
        if (conversation.Messages.Count() == 2) await Intercept(kernel, conversation, cancellationToken);

        conversation.StopRequested = true;
        return conversation;
    }
 

    private  async Task Intercept(Kernel kernel, Conversation conversation, CancellationToken cancellationToken)
    {

        var config = await _modelConfigurationService.GetConfig();
       kernel  = await _kernelService.CreateKernelAsync(config.Provider, config.Model, functionInvocationFilters: new List<IFunctionInvocationFilter>(){new FunctionCallingFilter()});


        await LoadPluginsAsync(kernel);

        var prompt = conversation.Messages.Last().Content;
        var lastMsg = new ConversationMessage("assistant", "Executing....");
        conversation.Messages.Add(lastMsg);
        OnUpdate?.Invoke();

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

            ChatHistory chatHistory = [];
            chatHistory = [];


            chatHistory.AddUserMessage(prompt);

            var response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel, cancellationToken: cancellationToken);



            //if (response?.Metadata?["FinishReason"]?.ToString() == "stop")
            //{
            //    Logger.LogError("STOP");
            //}

            //Logger.LogInformation("FinishReason: " + response?.Metadata?["FinishReason"]);

            lastMsg.Content = response.Content;
        }
        catch (Exception e)
        {
            lastMsg.Content = e.Message + "\n";
            OnUpdate?.Invoke();
        }
    }

    private async Task LoadPluginsAsync(Kernel kernel)
    {
        var semanticPlugins = await _pluginsRepository.GetSemanticPlugins();

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

    }
}