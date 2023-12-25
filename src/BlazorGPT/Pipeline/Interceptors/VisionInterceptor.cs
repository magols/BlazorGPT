using Blazored.LocalStorage;
using BlazorGPT.Shared.PluginSelector;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace BlazorGPT.Pipeline.Interceptors;

public class VisionInterceptor : InterceptorBase, IInterceptor
{
    private CancellationToken _cancellationToken;
    private PluginsRepository _pluginsRepository;
    private ILocalStorageService? _localStorageService;
    private KernelService _kernelService;
    private IServiceProvider _serviceProvider;

    public VisionInterceptor(IDbContextFactory<BlazorGptDBContext> context,
        ConversationsRepository conversationsRepository,
        PluginsRepository pluginsRepository,
            ILocalStorageService localStorageService,
        KernelService kernelService,
        IServiceProvider serviceProvider) : base(context, conversationsRepository)
    {
        _serviceProvider = serviceProvider;
        _kernelService = kernelService;
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

       
    }
   
}