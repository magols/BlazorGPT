using System.Text.RegularExpressions;
using BlazorGPT.Data;
using BlazorGPT.Data.Model;

namespace BlazorGPT.Managers;

public abstract class InterceptorBase
{
    private IDbContextFactory<BlazorGptDBContext> _context;
    private ConversationsRepository _conversationsRepository;

    public InterceptorBase(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository)
    {
        _conversationsRepository = conversationsRepository;
        _context = context;
    }

    protected async Task ParseMessageAndSaveState(ConversationMessage lastMsg, string? stateType)
    {
        var pattern = @"\[STATEDATA\](.*?)\[/STATEDATA\]";
        var matches = Regex.Matches(lastMsg.Content, pattern,
            RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (matches.Any())
        {
            var state = matches.First().Groups[1].Value;

            var savedState = new MessageState()
            {
                Type = stateType,
                Content = state,
                IsPublished = true,
                Name = "msgstate",
                MessageId = lastMsg.Id
            };
            lastMsg.State = savedState;

            // save to message state
            var ctx = await _context.CreateDbContextAsync();

            

            ctx.Entry(lastMsg).State = EntityState.Modified;
            ctx.Entry(savedState).State = EntityState.Added;

            // save to root conversation HiveState
            Conversation? root = null;
            root = _conversationsRepository.GetMergedBranchRootConversation((Guid)lastMsg.ConversationId);
            if (root != null)
            {
                var hiveState = new HiveState()
                {
                    Type = stateType,
                    Content = savedState.Content,
                    IsPublished = savedState.IsPublished,
                    Name = savedState.Name,
                    ConversationId = root.Id
                };
                root.HiveState = hiveState;
                ctx.Entry(hiveState).State = EntityState.Added;


            }


            // set to conversation tree state

            var treeState = new ConversationTreeState()
            {
                Type = stateType,
                Content = savedState.Content,
                IsPublished = savedState.IsPublished,
                Name = savedState.Name,
                ConversationId = lastMsg.ConversationId
            };

            lastMsg.Conversation!.TreeStateList.Add(treeState);
            ctx.Entry(lastMsg.Conversation).State = EntityState.Modified;
            ctx.Entry(treeState).State = EntityState.Added;

            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}