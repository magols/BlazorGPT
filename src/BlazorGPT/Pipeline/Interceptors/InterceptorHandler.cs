using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlazorGPT.Pipeline.Interceptors;

public class InterceptorHandler : IInterceptorHandler
{
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;

    private PipelineOptions? pipelineOptions = null;
    private readonly PipelineOptions _options;

    public InterceptorHandler(IServiceProvider serviceProvider, IConfiguration configuration, IOptions<PipelineOptions> options)
    {
        _options = options.Value;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _configuration.Bind("PipelineOptions", pipelineOptions);
    }

    private IEnumerable<IInterceptor> Interceptors => _serviceProvider.GetServices<IInterceptor>();

    private IEnumerable<IInterceptor> EnabledInterceptors => Interceptors.Where(i => _options.EnabledInterceptors != null && _options.EnabledInterceptors.Contains(i.Name));
        
    public async Task<Conversation> Send(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null)
    {
        IEnumerable<IInterceptor> enabled = enabledInterceptors != null ? Interceptors.Where(enabledInterceptors.Contains) : EnabledInterceptors;
        foreach (var interceptor in enabled)
        {
            conversation = await interceptor.Send(conversation);
        }
        return conversation;

    }

    public async Task<Conversation> Receive(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors)
    {
        IEnumerable<IInterceptor> enabled = enabledInterceptors != null ? Interceptors.Where(enabledInterceptors.Contains) : EnabledInterceptors;

        // if the StateFileSaveInterceptor is enabled Recieve with that one first
        if (enabled.Any(i => i.Name == "Save file"))
        {
            var stateFileSaveInterceptor = enabled.First(i => i.Name == "Save file");

            await stateFileSaveInterceptor.Receive(conversation);
        }

        foreach (var interceptor in enabled.Where(e => e.Name != "Save File"))
        {
            conversation = await interceptor.Receive(conversation);
        }

        return conversation;
    }
}