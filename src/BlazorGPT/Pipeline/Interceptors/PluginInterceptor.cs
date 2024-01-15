using Blazored.LocalStorage;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace BlazorGPT.Pipeline.Interceptors;

public class PluginInterceptor : InterceptorBase, IInterceptor
{
    private CancellationToken _cancellationToken;
    private PluginsRepository _pluginsRepository;
    private ILocalStorageService? _localStorageService;
    private KernelService _kernelService;
    private IServiceProvider _serviceProvider;

    public PluginInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _kernelService = _serviceProvider.GetRequiredService<KernelService>();
        _localStorageService = _serviceProvider.GetRequiredService<ILocalStorageService>();
        _pluginsRepository = _serviceProvider.GetRequiredService<PluginsRepository>();
    }

    public string Name { get; } = "Plugins";
    public bool Internal { get; } = false;

    public Task<Conversation> Receive(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(conversation);
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        _cancellationToken = cancellationToken;
        if (conversation.Messages.Count() == 2)
        {
            await Intercept(kernel, conversation);
        }

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

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });

        try
        {
            var plan = await planner.CreatePlanAsync(kernel, ask, _cancellationToken);
            conversation.SKPlan = plan.ToString();

            lastMsg.Content = "Executing plan...";
            conversation.PluginsNames = string.Join(",", kernel.Plugins.Select(o => o.Name));
            OnUpdate?.Invoke();

            KernelArguments args = new KernelArguments();
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
        var semanticPlugins = await  _pluginsRepository.GetFromDiskAsync();

        List<string> nativePlugins = new List<string>();
        // todo: bad to read from browser here but here we are
        if (_localStorageService != null)
        {
          var enabledPlugins = await _localStorageService.GetItemAsync<List<Plugin>>("bgpt_plugins", _cancellationToken);
            var enabledNames = enabledPlugins.Select(o => o.Name);
            semanticPlugins = semanticPlugins.Where(o => enabledNames.Contains(o.Name)).ToList();

            var native = enabledPlugins.Where(o => o.Name.LastIndexOf(".") > 1);
            nativePlugins = native.Select(o => o.Name).ToList();
 
        }

        foreach (var plugin in semanticPlugins)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Plugins", plugin.Name);
            kernel.ImportPluginFromPromptDirectory(path, plugin.Name);
        }

        foreach (var className in nativePlugins)
        {
            // instantiate the class
            var type = Type.GetType(className);
            if (type != null)
            {
                var instance = Activator.CreateInstance(type, _serviceProvider );
                try
                {
                    var pluginName = className.Substring(className.LastIndexOf(".") + 1);
                    kernel.ImportPluginFromObject(instance, pluginName);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Could not load native plugins", e);
                }
            }
        }
    }
}