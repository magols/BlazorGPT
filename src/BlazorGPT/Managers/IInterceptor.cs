using BlazorGPT.Data;
using BlazorGPT.Data.Model;

namespace BlazorGPT.Managers;

public interface IInterceptor
{
    string Name { get; }
    bool Internal { get; }
    Task<Conversation> Receive(Conversation conversation);
    Task<Conversation> Send(Conversation conversation);
}