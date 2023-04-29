using System.Text.RegularExpressions;
using BlazorGPT.Data;
using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Managers;

public class StateFileSaveInterceptor : InterceptorBase, IInterceptor
{
    private string path = @"C:\source\BlazorGPT\BlazorGPT\wwwroot\state\";

    public StateFileSaveInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository) : base(context, conversationsRepository)
    {
    }

    public string Name { get; } = "Save file";
    public bool Internal { get; } = true;

    public async Task<Conversation> Receive(Conversation conversation)
    {
        await ParseMessageAndSaveStateToDisk(conversation.Messages.Last());
        return conversation;
    }

    public async Task<Conversation> Send(Conversation conversation)
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
            
                await File.WriteAllTextAsync(path  + lastMsg.Id, state);
          
        }
    }
}