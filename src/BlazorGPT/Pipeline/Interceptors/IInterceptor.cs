namespace BlazorGPT.Pipeline.Interceptors;

public interface IInterceptor
{
    string Name { get; }
    bool Internal { get; }
    Task<Conversation> Receive(Conversation conversation);
    Task<Conversation> Send(Conversation conversation);
}