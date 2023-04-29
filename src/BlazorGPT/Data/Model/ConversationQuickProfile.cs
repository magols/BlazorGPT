namespace BlazorGPT.Data.Model;

public class ConversationQuickProfile
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; }

    public Guid QuickProfileId { get; set; }
    public QuickProfile QuickProfile { get; set; }
}