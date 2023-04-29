using BlazorGPT.Data.Model;

namespace BlazorGPT.Data;

public static class DataExtensions
{
    public static bool HasStarted(this Conversation conversation)
    {
        return conversation.Messages.Any(m => m.Role == "assistant");
    }

    public static bool InitStage(this Conversation conversation)
    {
        return conversation.Messages.Last().Role == "assistant"
               && conversation.Messages.Count(m => m.Role == "assistant") == 1;
    }
}