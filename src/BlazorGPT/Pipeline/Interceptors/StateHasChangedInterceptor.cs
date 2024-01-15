using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorGPT.Pipeline.Interceptors;

public class StateHasChangedInterceptor : InterceptorBase, IInterceptor
{
    private readonly StateHasChangedInterceptorService _stateHasChangedInterceptorService;

    public StateHasChangedInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {

        _stateHasChangedInterceptorService = serviceProvider.GetRequiredService<StateHasChangedInterceptorService>();
    }

    public bool Internal { get; } = true;

    public string Name { get; } = "State has changed";
    public async Task<Conversation> Receive(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        await ParseAndSendNotification(conversation.Messages.Last());

        return conversation;
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
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