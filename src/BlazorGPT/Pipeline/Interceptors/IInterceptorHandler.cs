using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public interface IInterceptorHandler
{
    Task<Conversation> Send(IKernel kernel, Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors = null, CancellationToken cancellationToken = default);
    Task<Conversation> Receive(IKernel kernel, Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors = null, CancellationToken cancellationToken = default);

    Func<Task>? OnUpdate { get; set; }
}