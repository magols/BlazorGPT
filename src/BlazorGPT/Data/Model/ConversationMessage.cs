using OpenAI.GPT3.ObjectModels.RequestModels;

namespace BlazorGPT.Data.Model;

public class ConversationMessage : ChatMessage
{


    public ConversationMessage(string role, string content) : base(role, content)
    {
    }

    public ConversationMessage(ChatMessage msg) : base(msg.Role, msg.Content)
    {
    }

    public Guid? Id { get; set; }
    public Conversation? Conversation { get; set; }
    public Guid? ConversationId { get; set; }

    public MessageState? State { get; set; }

    public ICollection<Conversation> BranchedConversations { get; set; } = new List<Conversation>();

    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

}