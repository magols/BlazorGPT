namespace BlazorGPT.Pipeline.Interceptors;

public interface IInterceptorHandler
{
    Task<Conversation> Send(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null);
    Task<Conversation> Receive(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null);
}