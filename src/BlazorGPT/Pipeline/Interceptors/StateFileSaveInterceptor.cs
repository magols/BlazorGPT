using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline.Interceptors;

public class StateFileSaveInterceptor : InterceptorBase, IInterceptor
{
    private readonly string _path;

    public StateFileSaveInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository, IOptions<PipelineOptions> options
    ) : base(context, conversationsRepository)
    {
        _path = options.Value.StateFileSaveInterceptorPath;
    }

    public string Name { get; } = "Save file";
    public bool Internal { get; } = true;

    public async Task<Conversation> Receive(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        await ParseMessageAndSaveStateToDisk(conversation.Messages.Last());
        return conversation;
    }

    public async Task<Conversation> Send(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        return conversation;
    }

    async Task ParseMessageAndSaveStateToDisk(ConversationMessage lastMsg)
    {
        var pattern = @"\[STATEDATA\](.*?)\[/STATEDATA\]";
        var matches = Regex.Matches(lastMsg.Content, pattern,
            RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (matches.Any())
        {
            var state = matches.First().Groups[1].Value;
            
                await File.WriteAllTextAsync( Path.Join(_path, lastMsg.Id.ToString()), state);
          
        }
    }
}