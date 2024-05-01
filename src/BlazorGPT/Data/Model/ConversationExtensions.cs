namespace BlazorGPT.Data.Model;

public static class ConversationExtensions
{
    public static bool IsStarted(this Conversation conversation)
    {
        return conversation.Messages.Count > 1;
    }

    // last message was from assistant
    public static bool IsAssistantTurn(this ConversationMessage message)
    {
        return message.Role == ConversationRole.Assistant;
    }

    public static ConversationMessage? GetSystemMessage(this Conversation conversation) 
    {
        return conversation.Messages.FirstOrDefault(o => o.Role == "system");
    }

    public static void SetSystemMessage(this Conversation conversation, string systemMessage)
    {
        if (conversation.Messages.Count == 0)
        {
            conversation.Messages.Add(new ConversationMessage("system", systemMessage));
            return;
        } 
        conversation.Messages.First(o => o.Role == "system").Content = systemMessage;
    }
}