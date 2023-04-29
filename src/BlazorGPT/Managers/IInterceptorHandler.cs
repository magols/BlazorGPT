using BlazorGPT.Data;
using BlazorGPT.Data.Model;

namespace BlazorGPT.Managers;

public interface IInterceptorHandler
{
    Task<Conversation> Send(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null);
    Task<Conversation> Receive(Conversation conversation, IEnumerable<IInterceptor>? enabledInterceptors = null);
}