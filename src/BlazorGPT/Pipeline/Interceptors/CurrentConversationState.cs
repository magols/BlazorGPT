using Microsoft.Extensions.Caching.Memory;

namespace BlazorGPT.Pipeline.Interceptors;

public class CurrentConversationState(IMemoryCache stateCache)
{

    public void SetCurrentConversationForUser(Conversation conversation)
    {
        string key = $@"cstate{conversation.UserId}";
        stateCache.Set(key, conversation);
    }

    public   Conversation? GetCurrentConversation(string userId)
    {
        string key = $@"cstate{userId}";

        stateCache.TryGetValue(key, out Conversation? conversation);
        return conversation;
    }

    public void RemoveCurrentConversation(string userId)
    {
        string key = $@"cstate{userId}";
        stateCache.Remove(key);
    }
}