using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public interface IInterceptorHandler
{

    Func<Task>? OnUpdate { get; set; }


    Task<Conversation> Receive(Kernel kernel,
        Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors = null,
        List<string>? enabledInterceptorNames = null,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken? cancellationToken = default);

    Task<Conversation> Send(Kernel kernel, 
        Conversation conversation,
        IEnumerable<IInterceptor>? enabledInterceptors = null,
        List<string>? enabledInterceptorNames = null,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken? cancellationToken = default);
}