using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using ChatMessage = Azure.AI.OpenAI.ChatMessage;


namespace BlazorGPT.Data.Model;

public class ConversationMessage
{


    public ConversationMessage(string role, string content)  
    {
        Role = role;
        Content = content;
    }

    public ConversationMessage(ChatMessage msg)  
    {
        Role = msg.Role == ChatRole.User ? "user" : "assistant";
        Content = msg.Content;
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

}