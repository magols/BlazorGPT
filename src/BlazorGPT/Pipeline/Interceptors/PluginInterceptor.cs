using Blazored.LocalStorage;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace BlazorGPT.Pipeline.Interceptors;

public class PluginInterceptor : InterceptorBase, IInterceptor
{
    private CancellationToken _cancellationToken;
    private KernelService _kernelService;
    private readonly ILocalStorageService? _localStorageService;
    private readonly PluginsRepository _pluginsRepository;
    private readonly IServiceProvider _serviceProvider;

    public PluginInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kernelService = _serviceProvider.GetRequiredService<KernelService>();
        _localStorageService = _serviceProvider.GetRequiredService<ILocalStorageService>();
        _pluginsRepository = _serviceProvider.GetRequiredService<PluginsRepository>();
    }

    public override string Name { get; } = "Plugins with Handlebars Planner";
    public bool Internal { get; } = false;

    public Task<Conversation> Receive(Kernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(conversation);
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        if (conversation.Messages.Count() == 2) await Intercept(kernel, conversation);

        conversation.StopRequested = true;
        return conversation;
    }

    private async Task Intercept(Kernel kernel, Conversation conversation)
    {
        await LoadPluginsAsync(kernel);

        var ask = conversation.Messages.Last().Content;
        var lastMsg = new ConversationMessage("assistant", "Constructing plan...");
        conversation.Messages.Add(lastMsg);
        OnUpdate?.Invoke();

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions { AllowLoops = true });

        try
        {
            var plan = await planner.CreatePlanAsync(kernel, ask, _cancellationToken);
            conversation.SKPlan = plan.ToString();

            lastMsg.Content = "Executing plan...";
            conversation.PluginsNames = string.Join(",", kernel.Plugins.Select(o => o.Name));
            OnUpdate?.Invoke();

            var args = new KernelArguments();
            var result = await plan.InvokeAsync(kernel, args, _cancellationToken);

            lastMsg.Content = result;
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