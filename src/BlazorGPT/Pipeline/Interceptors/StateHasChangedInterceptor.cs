using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorGPT.Pipeline.Interceptors;

public class StateHasChangedInterceptor(IServiceProvider serviceProvider)
    : InterceptorBase(serviceProvider), IInterceptor
{
    private readonly StateHasChangedInterceptorService _stateHasChangedInterceptorService = serviceProvider.GetRequiredService<StateHasChangedInterceptorService>();

    public override bool Internal { get; } = true;

    public override string Name { get; } = "State has changed";

    public override async  Task<Conversation> Receive(Kernel kernel, Conversation conversation, Func<string, Task<string>>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        await ParseAndSendNotification(conversation.Messages.Last());

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