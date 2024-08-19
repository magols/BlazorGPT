namespace BlazorGPT.Data.Model;

public class ConversationMessage
{
    public ConversationMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }


    public Guid? Id { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid? ConversationId { get; set; }

    public MessageState? State { get; set; }

    public ICollection<Conversation> BranchedConversations { get; set; } = new List<Conversation>();

    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string? ActionLog { get; set;  }
}