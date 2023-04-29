using System.Text.RegularExpressions;

namespace BlazorGPT.Pipeline.Interceptors;

public class StateHasChangedInterceptor : InterceptorBase, IInterceptor
{
    private StateHasChangedInterceptorService _stateHasChangedInterceptorService;

    public StateHasChangedInterceptor(StateHasChangedInterceptorService stateHasChangedInterceptorService, IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository) : base(context, conversationsRepository)
    {
        _stateHasChangedInterceptorService = stateHasChangedInterceptorService;
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

            await _stateHasChangedInterceptorService.ConversationUpdated((Guid)lastMsg.ConversationId);
        }
    }
}