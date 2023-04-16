using System.Text.RegularExpressions;
using BlazorGPT.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Managers;

public class StateHasChangedInterceptor : InterceptorBase, IInterceptor
{
    private StateManager _stateManager;

    public StateHasChangedInterceptor(StateManager stateManager, IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository) : base(context, conversationsRepository)
    {
        _stateManager = stateManager;
    }

    public bool Internal { get; } = true;

    public string Name { get; } = "State has changed";
    public async Task<Conversation> Receive(Conversation conversation)
    {
        await ParseAndSendNotification(conversation.Messages.Last());

        return conversation;
    }

    public async Task<Conversation> Send(Conversation conversation)
    {
        return conversation;
    }

    private async Task ParseAndSendNotification(ConversationMessage lastMsg)
    {
        var pattern = @"\[STATEDATA\](.*?)\[/STATEDATA\]";
        var matches = Regex.Matches(lastMsg.Content, pattern,
            RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (matches.Any())
        {

            await _stateManager.ConversationUpdated((Guid)lastMsg.ConversationId);
        }
    }
}