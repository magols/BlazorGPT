using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorGPT.Data.Model;

public class Conversation
{
    public Guid? Id { get; set; }

    [NotMapped]
    public bool StopRequested { get; set; }

    [Required]
    public string Model { get; set; } = null!;

    [Required]
    public string? UserId { get; set; } = null!;

    public List<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    public List<QuickProfile> QuickProfiles { get; set; } = new List<QuickProfile>();

    public List<string>? FileUrls { get; set; } = new();

    public string? Summary { get; set; }

    public string? SKPlan { get; set; } = null!;
    public string? PluginsNames { get; set; } = null!;


    public DateTime DateStarted { get; set; }

    public Guid? BranchedFromMessageId { get; set; }
    public ConversationMessage? BranchedFromMessage { get; set; }

    public List<ConversationTreeState> TreeStateList { get; set; } = new();

    [NotMapped]
    public Conversation HiveConversation { get; set; }

    public HiveState? HiveState
    { get; set; }

    public void AddMessage(ConversationMessage message)
    {
        message.ConversationId = Id;
        Messages.Add(message);
    }

    public void AddMessage(string role, string content)
    {
        AddMessage(new ConversationMessage(role, content));
    }   

    public static Conversation CreateConversation(string model, string userId, string systemMessage, string? message = null)
    {
        Conversation conversation = new Conversation
        {
            Model = model,
            UserId = userId,
            DateStarted = DateTime.Now
        };
      
        conversation.AddMessage(new ConversationMessage("system", systemMessage));
        if (message != null)
        {
            conversation.AddMessage(new ConversationMessage("user", message));
        }
        return conversation;
    }
 
}

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

}