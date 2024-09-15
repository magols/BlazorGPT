using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public class SampleInterceptor(IServiceProvider serviceProvider) : InterceptorBase(serviceProvider), IInterceptor
{
    public override string Name { get; } = "Sample interceptor";

    public override Task<Conversation> Send(Kernel kernel, Conversation conversation,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        conversation.Messages.Last().Content += " [SampleInterceptor] send";
        return Task.FromResult(conversation);
    }

    public override Task<Conversation> Receive(Kernel kernel, Conversation conversation,
        Func<string, Task<string>>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(conversation);
    }
}