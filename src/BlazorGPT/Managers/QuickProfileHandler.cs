using BlazorGPT.Data;
using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace BlazorGPT.Managers;

public class QuickProfileHandler : IQuickProfileHandler
{
    private IDbContextFactory<BlazorGptDBContext> _dbContextFactory;

    public QuickProfileHandler(IDbContextFactory<BlazorGptDBContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Conversation> Send(Conversation conversation, IEnumerable<QuickProfile>? beforeProfiles = null)
    {

        if (!conversation.HasStarted())
        {
            var selected = beforeProfiles.Where(p => p.InsertAt == InsertAt.Before);
            if (selected.Any())
            {
                string startMsg = string.Join(" ", selected.Select(p => p.Content)) + "\n\n\n";

                conversation.Messages.Insert(1, new ConversationMessage(ChatMessage.FromUser(startMsg)));

            }
        }

        return conversation;
    }

    public  async Task<Conversation> Receive(ChatWrapper chatWrapper, Conversation conversation,
        IEnumerable<QuickProfile>? profiles = null)
    {

        await using var ctx = await _dbContextFactory.CreateDbContextAsync();

        if (conversation.Id == null || conversation.Id == default(Guid))
        {
            foreach (var p in profiles.Where(p => p.Id != default))
            {
                ctx.Attach(p);
                conversation.QuickProfiles.Add(p);
            }
        }

        if (conversation.InitStage())
        {
            ;
            if (profiles.Any())
            {
                foreach (var profile in profiles)
                {
                    conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser(profile.Content)));
                    conversation = await chatWrapper.Send(conversation).ConfigureAwait(true);

                }
            }
        }

        return conversation;

    }
}