using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;

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