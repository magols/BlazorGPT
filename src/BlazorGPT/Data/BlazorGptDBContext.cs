using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace BlazorGPT.Data;

public class BlazorGptDBContext : DbContext
{
    public BlazorGptDBContext(DbContextOptions<BlazorGptDBContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationMessage> Messages { get; set; }

    public DbSet<Script> Scripts { get; set; }

    public DbSet<ScriptStep> ScriptSteps { get; set; }

    public DbSet<QuickProfile> QuickProfiles { get; set; }
    public DbSet<ConversationQuickProfile> ConversationQuickProfiles { get; set; }
    public DbSet<MessageState> StateData { get; set; }
    public DbSet<ConversationTreeState> TreeStateData { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationMessage>()
            .HasMany(p => p.BranchedConversations)
            .WithOne(p => p.BranchedFromMessage)
            .HasForeignKey(p => p.BranchedFromMessageId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.ClientNoAction);

        

        modelBuilder.Entity<Conversation>()
            .HasMany(p => p.Messages)
            .WithOne(b => b.Conversation)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConversationMessage>()
            .OwnsOne(p => p.State)
            .WithOwner(P => P.Message)
            .HasForeignKey(p => p.MessageId);

        modelBuilder.Entity<Conversation>()
            .OwnsMany(p => p.TreeStateList)
            .WithOwner(P => P.Conversation)
            .HasForeignKey(p => p.ConversationId);

        modelBuilder.Entity<Conversation>()
            .OwnsOne(p => p.HiveState)
            .WithOwner(P => P.Conversation)
            .HasForeignKey(p => p.ConversationId);

        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.QuickProfiles)
            .WithMany(q => q.Conversations)
            .UsingEntity<ConversationQuickProfile>(
                j => j
                    .HasOne(cp => cp.QuickProfile)
                    .WithMany()
                    .HasForeignKey(cp => cp.QuickProfileId),
                j => j
                    .HasOne(cp => cp.Conversation)
                    .WithMany()
                    .HasForeignKey(cp => cp.ConversationId),
                j =>
                {
                    j.HasKey(cp => new { cp.ConversationId, cp.QuickProfileId });
                    j.ToTable("ConversationQuickProfiles");
                });
    }
}

public abstract class StateDataBase
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Content { get; set; }

    public string? Type { get; set; } = null!;
    public bool IsPublished { get; set; }
}

public class MessageState : StateDataBase {
    public Guid? MessageId { get; set; }
    public ConversationMessage? Message { get; set; }
}

public class ConversationTreeState : StateDataBase
{
    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
}

public class HiveState : StateDataBase
{
    public Guid? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
}

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

public class ConversationQuickProfile
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; }

    public Guid QuickProfileId { get; set; }
    public QuickProfile QuickProfile { get; set; }
}



public class QuickProfile
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Content { get; set; }

    public InsertAt InsertAt { get; set; }

    public string? UserId { get; set; }

    public bool EnabledDefault { get; set; }
    public List<Conversation>? Conversations { get; set; }
}


public class Conversation
{
    public Guid? Id { get; set; }
    [Required]
    public string? UserId { get; set; } = null!;

    public List<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();

    public List<QuickProfile> QuickProfiles { get; set; } = new List<QuickProfile>();


    public string? Summary { get; set; }


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


    public static Conversation CreateConversation(string userId, string systemMessage, string? message = null)
    {
        Conversation conversation = new Conversation
        {
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