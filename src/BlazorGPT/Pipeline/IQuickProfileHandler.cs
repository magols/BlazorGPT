using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public interface IQuickProfileHandler
{
    Task<Conversation> Send(IKernel kernel, Conversation conversation, IEnumerable<QuickProfile>? beforeProfiles = null);
    Task<Conversation> Receive(IKernel kernel, ChatWrapper chatWrapper, Conversation conversation,
        IEnumerable<QuickProfile>? profiles = null);
}