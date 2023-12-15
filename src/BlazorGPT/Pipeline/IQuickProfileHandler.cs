using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public interface IQuickProfileHandler
{
    Task<Conversation> Send(Kernel kernel, Conversation conversation, IEnumerable<QuickProfile>? beforeProfiles = null);
    Task<Conversation> Receive(Kernel kernel, ChatWrapper chatWrapper, Conversation conversation,
        IEnumerable<QuickProfile>? profiles = null);
}