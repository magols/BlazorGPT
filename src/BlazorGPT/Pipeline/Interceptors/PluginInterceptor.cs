using Blazored.LocalStorage;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace BlazorGPT.Pipeline.Interceptors;

public class PluginInterceptor : InterceptorBase, IInterceptor
{
    private CancellationToken _cancellationToken;
    private PluginsRepository _pluginsRepository;
    private ILocalStorageService? _localStorageService;

    public PluginInterceptor(IDbContextFactory<BlazorGptDBContext> context,
        ConversationsRepository conversationsRepository,
        PluginsRepository pluginsRepository,
            ILocalStorageService localStorageService) : base(context, conversationsRepository)
    {
        _localStorageService = localStorageService;
        _pluginsRepository = pluginsRepository;
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

        var lastMsg = new ConversationMessage("assistant", "....");
        conversation.Messages.Add(lastMsg);
        OnUpdate?.Invoke();

        var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions() { AllowLoops = true });
        var plan = await planner.CreatePlanAsync(kernel, ask, _cancellationToken);
        conversation.SKPlan = plan.ToString();

        conversation.PluginsNames = string.Join(",", kernel.Plugins.Select(o => o.Name));

        OnUpdate?.Invoke();

        Console.WriteLine($"Plan: {plan}");

        KernelArguments args = new KernelArguments();
        var result = await plan.InvokeAsync(kernel, args, _cancellationToken);

        Console.WriteLine($"Results: {result}");
        lastMsg.Content = result;

    }

    private async Task LoadPluginsAsync(Kernel kernel)
    {
        var semanticPlugins = await  _pluginsRepository.GetFromDiskAsync();

        // todo: bad to read from browser here but here we are
        if (_localStorageService != null)
        {
            try
            {
                var enabledPlugins = await _localStorageService.GetItemAsync<List<Plugin>>("bgpt_plugins", _cancellationToken);
                var enabledNames = enabledPlugins.Select(o => o.Name);
                semanticPlugins = semanticPlugins.Where(o => enabledNames.Contains(o.Name)).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
            }

        }

        foreach (var plugin in semanticPlugins)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "Plugins", plugin.Name);
            kernel.ImportPluginFromPromptDirectory(path, plugin.Name);
            Console.WriteLine("loading " + path);
        }
    }
}