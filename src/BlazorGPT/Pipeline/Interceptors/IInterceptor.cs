using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public interface IInterceptor
{
    string Name { get; }
    bool Internal { get; }
    Task<Conversation> Receive(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default);
    Task<Conversation> Send(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default);


}