using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public class InterceptorHandler : IInterceptorHandler
{
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;

    private PipelineOptions? pipelineOptions = null;
    private readonly PipelineOptions _options;



    public Func<Task>? OnUpdate { get; set; }


    public InterceptorHandler(IServiceProvider serviceProvider, IConfiguration configuration, IOptions<PipelineOptions> options)
    {
        _options = options.Value;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _configuration.Bind("PipelineOptions", pipelineOptions);
    }

    private IEnumerable<IInterceptor> Interceptors => _serviceProvider.GetServices<IInterceptor>();

    private IEnumerable<IInterceptor> EnabledInterceptors => Interceptors.Where(i => _options.EnabledInterceptors != null && _options.EnabledInterceptors.Contains(i.Name));
        
    public async Task<Conversation> Send(Kernel kernel, Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors = null, CancellationToken cancellationToken = default)
    {

        IEnumerable<IInterceptor> enabled = enabledInterceptors?.ToList() ?? EnabledInterceptors;

        foreach (var interceptor in enabled)
        {
            // check if the interceptor iinherits the abstract class InterceptorBase and if so set the OnUpdate function
            if (interceptor is InterceptorBase interceptorBase)
            {
                interceptorBase.OnUpdate = OnUpdate;
            }

            conversation = await interceptor.Send(kernel, conversation, cancellationToken);

            if (OnUpdate != null)
            {
                await OnUpdate();
            }
        }
        return conversation;

    }

    public async Task<Conversation> Receive(Kernel kernel, Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors, CancellationToken cancellationToken = default)
    {
        IEnumerable<IInterceptor> enabled = enabledInterceptors != null ? Interceptors.Where(enabledInterceptors.Contains) : EnabledInterceptors;

        // if the StateFileSaveInterceptor is enabled Recieve with that one first
        if (enabled.Any(i => i.Name == "Save file"))
        {
            var stateFileSaveInterceptor = enabled.First(i => i.Name == "Save file");

            await stateFileSaveInterceptor.Receive(kernel, conversation);
        }

        foreach (var interceptor in enabled.Where(e => e.Name != "Save File"))
        {
            conversation = await interceptor.Receive(kernel, conversation);
            if (OnUpdate != null)
            {
                await OnUpdate();
            }
        }

        return conversation;
    }

}