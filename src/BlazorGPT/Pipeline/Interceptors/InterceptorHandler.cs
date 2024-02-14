using BlazorGPT.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public class InterceptorHandler : IInterceptorHandler
{
    private IServiceProvider _serviceProvider;
    private IConfiguration _configuration;

    private PipelineOptions? pipelineOptions = null;
    private readonly PipelineOptions _options;
    private InterceptorRepository _interceptorRepository;


    public Func<Task>? OnUpdate { get; set; }



    public async Task<Conversation> Send(Kernel kernel, Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null,
        List<string>? enabledInterceptorNames = null,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken? cancellationToken = default)
    {

        CancellationToken ct = cancellationToken ?? CancellationToken.None;
        if (enabledInterceptors == null) enabledInterceptors = new List<IInterceptor>();
        if (enabledInterceptorNames == null) enabledInterceptorNames = new List<string>();

        List<IInterceptor> all = new List<IInterceptor>();
        all.AddRange(InternalInterceptors);
        all.AddRange(ExternalInterceptors);

        List<IInterceptor> selected = new List<IInterceptor>();

        selected = all.Where(i => enabledInterceptors.Contains(i)
        || enabledInterceptorNames.Contains(i.Name)
        || (_options.EnabledInterceptors != null &&_options.EnabledInterceptors.Contains(i.Name))
        ).ToList();


        foreach (var interceptor in selected)
        {
            if (interceptor is InterceptorBase interceptorBase)
            {
                interceptorBase.OnUpdate = OnUpdate;
            }

            conversation = await interceptor.Send(kernel, conversation, onComplete, ct);

            if (OnUpdate != null)
            {
                await OnUpdate();
            }
        }
        return conversation;
    }


    public async Task<Conversation> Receive(Kernel kernel, Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null,
        List<string>? enabledInterceptorNames = null,
        Func<string, Task<string>>? onComplete = null,

        CancellationToken? cancellationToken = default)
    {

        CancellationToken ct = cancellationToken ?? CancellationToken.None;

        List<IInterceptor> all = new List<IInterceptor>();
        all.AddRange(InternalInterceptors);
        all.AddRange(ExternalInterceptors);

        if (enabledInterceptorNames != null)
        {
            var interceptorNames = enabledInterceptorNames.ToList();
            if (interceptorNames.Any())
            {
                all = all.Where(i => interceptorNames.Contains(i.Name)).ToList();
            }
        }

        if (enabledInterceptors != null)
        {
            var interceptors = enabledInterceptors as IInterceptor[] ?? Enumerable.Empty<IInterceptor>();

            all = all.Intersect(interceptors.ToArray()).ToList();
        }

        foreach (var interceptor in all)
        {
            if (interceptor is InterceptorBase interceptorBase)
            {
                interceptorBase.OnUpdate = OnUpdate;
            }

            conversation = await interceptor.Receive(kernel, conversation, onComplete, ct);

            if (OnUpdate != null)
            {
                await OnUpdate();
            }
        }
        return conversation;

    }

    [Inject]
    public IServiceProvider ServiceProvider { get; set; } = null!;

    public InterceptorHandler(IServiceProvider serviceProvider, IConfiguration configuration, IOptions<PipelineOptions> options, InterceptorRepository interceptorRepository)
    {
        _interceptorRepository = interceptorRepository;
        _options = options.Value;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _configuration.Bind("PipelineOptions", pipelineOptions);
    }

    private IEnumerable<IInterceptor> InternalInterceptors =>_interceptorRepository.LoadInternal();

    private IEnumerable<IInterceptor> ExternalInterceptors => _interceptorRepository.LoadExternal();


    private IEnumerable<IInterceptor> InterceptorsFromOptions => InternalInterceptors.Where(i => _options.EnabledInterceptors != null && _options.EnabledInterceptors.Contains(i.Name));

}