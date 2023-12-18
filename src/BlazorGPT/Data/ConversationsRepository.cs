using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Data;

public class ConversationsRepository
{
    private readonly IDbContextFactory<BlazorGptDBContext> _dbContextFactory;
    private QuickProfileRepository _quickProfileRepository;

    public ConversationsRepository(IDbContextFactory<BlazorGptDBContext> dbContextFactory,
        QuickProfileRepository quickProfileRepository)
    {
        _quickProfileRepository = quickProfileRepository;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<Conversation>> GetConversationsByUserId(string userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var res = ctx.Conversations
            .Include(p => p.QuickProfiles)
            .Include(c => c.BranchedFromMessage)
            .Where(c => c.UserId == userId && c.Summary != null)
            .OrderByDescending(c => c.DateStarted)
            .ToList();

        return res;
    }

    // delete all conversations for a user
    public async Task DeleteConversationsByUserId(string userId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var res = ctx.Conversations
            .Where(c => c.UserId == userId)
            .ToList();
        ctx.Conversations.RemoveRange(res);
        await ctx.SaveChangesAsync();
    }

    public async Task<bool> DeleteConversation(Guid conversationId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var c = ctx.Conversations!
            .Include(c => c.Messages)
            .ThenInclude(m => m.BranchedConversations)
            .Include(p => p.BranchedFromMessage)
            .FirstOrDefault(c => c.Id == conversationId);


        var branchedConversations = c.Messages
            .Where(m => m.BranchedConversations.Any())
            .SelectMany(m => m.BranchedConversations)
            .Distinct();

        var strParam = string.Join("','", branchedConversations.Select(p => p.Id));

        if (branchedConversations.Any())
        {
            // on ctx execute a sql statement to set the column to null
            var command = $"UPDATE conversations SET BranchedFromMessageId = NULL where Id IN ('{strParam}') ";
            // execute a sql statement
            await ctx.Database.ExecuteSqlRawAsync(command);
        }

        ctx.Conversations.Remove(c);
        await ctx.SaveChangesAsync();
        return true;
    }

    // save a new conversation
    public async Task<Conversation> SaveConversation(Conversation conversation)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        foreach (var p in conversation.QuickProfiles)
        {
            var ca = ctx.Attach(p).Entity;
        }
        //foreach (var p in conversation.Messages)
        //{
        //    var ca = ctx.Attach(p).Entity;
        //}
        ctx.Conversations.Add(conversation);
        await ctx.SaveChangesAsync();
        return conversation;
    }
        

    public async Task UpdateConversation(Conversation conversation)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        ctx.Conversations.Update(conversation);
        await ctx.SaveChangesAsync();
    }

    public async Task DeleteMessages(List<ConversationMessage> messagesToRemove)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var convos = messagesToRemove.Where(c => c.Conversation != null)
            .Select(m => (Conversation)m.Conversation!)
            .Distinct()
            .ToArray();

        if (convos.Any())
        {
            // on ctx execute a sql statement to set the column to null
             var ids = messagesToRemove.Select(c => c.Id);

            var command = $"UPDATE conversations SET BranchedFromMessageId = NULL where BranchedFromMessageId IN ('{ string.Join("','", ids) }') ";
            // execute a sql statement
            await ctx.Database.ExecuteSqlRawAsync(command);
        }

        ctx.Messages.RemoveRange(messagesToRemove);
        await ctx.SaveChangesAsync();
    }

    public async Task<Conversation?> GetConversation(Guid? conversationId)
    {
        await using var ctx = await _dbContextFactory.CreateDbContextAsync();
        var c = ctx.Conversations
            .Include(c => c.Messages)
            .ThenInclude(m => m.BranchedConversations)
            .Include(c => c.QuickProfiles)
            .Include(c => c.BranchedFromMessage)
            .ThenInclude(m => m.Conversation)
            .Include(c => c.TreeStateList)
            .FirstOrDefault(c => c.Id == conversationId);
        return c;
    }

    public async Task<Conversation> BranchFromMessage(string userId, ConversationMessage msg, string newName, Conversation  conversation)
    {
        var nc = new Conversation();
        nc.UserId = userId;
        nc.Model  = conversation.Model;
        nc.Id = null;
        nc.BranchedFromMessageId = msg.Id;
        nc.BranchedFromMessage = null;
        nc.DateStarted = DateTime.Now;
        nc.Summary = newName;
        nc.Messages = conversation.Messages.ToList();
        var index = conversation.Messages.IndexOf(msg);
        nc.Messages.RemoveRange(index + 1, nc.Messages.Count - index - 1);

        // restart conversation by removing all messages after this one

        // clear messages id's
        List<ConversationMessage> copiedMessages = new List<ConversationMessage>();
        foreach (var message in nc.Messages)
        {
            if (message.Id == msg.Id)
            {
                var thisId = msg.Id;

            }
            var newmsg = new ConversationMessage(message.Role, message.Content);

            message.Id = null;
            message.ConversationId = null;
            message.Conversation = null;


            copiedMessages.Add(newmsg);
        }

        nc.Messages = copiedMessages;

     
        foreach (var qp in conversation.QuickProfiles)
        {
            qp.Conversations = null;
            nc.QuickProfiles.Add(qp);
        }

        var newConvo = await this.SaveConversation(nc);



        return newConvo;
    }


    public async Task UpdateMessageContent(Guid id, string content)
    {
        var ctx = await _dbContextFactory.CreateDbContextAsync();
        var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);
        if (msg != null)
        {
            msg.Content = content;
            await ctx.SaveChangesAsync();
        }
    }

    public async Task UpdateMessage(ConversationMessage msg)
    {
        var ctx = await _dbContextFactory.CreateDbContextAsync();
        // update message in database
        ctx.Attach(msg);

      //  ctx.Update(msg);
        await ctx.SaveChangesAsync();
    }

    public async Task<ConversationMessage> GetMessage(Guid stateId)
    {
        var ctx = await _dbContextFactory.CreateDbContextAsync();

        return ctx.Messages.Include(m => m.Conversation).First(m => m.Id == stateId);
    }

    public Conversation? GetMergedBranchRootConversation(Guid conversationId)
    {
        using var ctx = _dbContextFactory.CreateDbContext();
        var conversation = ctx.Conversations
            .Include(c => c.BranchedFromMessage)
            .ThenInclude(m => m.Conversation)
            .FirstOrDefault(c => c.Id == conversationId);

        if (conversation?.BranchedFromMessage?.Conversation != null)
        {
            return GetMergedBranchRootConversation(conversation.BranchedFromMessage?.Conversation);
        }

        return conversation;
    }

    public Conversation GetMergedBranchRootConversation(Conversation conversation)
    {

        //var parent = GetParentConverstion(conversation);
        if (conversation.BranchedFromMessage == null)
        {
            return conversation;
        }
        else
        {
            using var ctx =   _dbContextFactory.CreateDbContext();

            if (conversation.BranchedFromMessage.Conversation == null)
            {
                conversation.BranchedFromMessage.Conversation =
                    ctx.Conversations.Find(conversation.BranchedFromMessage.ConversationId);
            }

            return conversation;
        }
    }


}