using System.Text;
using BlazorGPT.Data;
using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Managers;

public class JsonStateInterceptor : InterceptorBase, IInterceptor, IStateWritingInterceptor
{

    public JsonStateInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository) : base(context, conversationsRepository)
    {
        Name = "Json Hive State";
    }

    public bool Internal => false;

    public string Name { get; }  


    public async Task<Conversation> Send(Conversation conversation)
    {

        if (conversation.Messages.Count() == 2)
        {
            await AppendInstruction(conversation);
        }

        return conversation;
    }

    private async Task<Conversation> AppendInstruction(Conversation conversation)
    {

        string state = "{\"id\":\"0\"}";


        var contentBuilder = new StringBuilder();
        contentBuilder.Append("[INSTRUCTION]Here is an empty json data as a model. We will iteratively add data to the json on our respective side. If either one has changed structure or values of the model we send over the json data enclosed in a [STATEDATA][/STATEDATA] tag. We will send it to eachother ONLY when data has changed! ");
        contentBuilder.Append(" Here is the base data. Remember from the start not to send it back unless you have changed structure or values.");
        contentBuilder.Append($" [STATEDATA]{state}[/STATEDATA][/INSTRUCTION]");
    
        var outgoing = conversation.Messages.ElementAt(1).Content;
        contentBuilder.AppendLine(outgoing);
        conversation.Messages.ElementAt(1).Content = contentBuilder.ToString();

        conversation.Messages.ElementAt(1).State = new MessageState()
        {
            Type = "JsonStateInterceptor",
            Content = state,
            IsPublished = true,
            Name = "msgstate",
        };

        return conversation;
    }

    private string path = @"C:\source\BlazorGPT\BlazorGPT\wwwroot\state\";

    public async Task<Conversation> Receive(Conversation conversation)
    {
        var newMessage = conversation.Messages.Last();
        await ParseMessageAndSaveState(newMessage, "JsonStateInterceptor");

        if (File.Exists(path + newMessage.Id))
        {
            File.Move(path + newMessage.Id, path + newMessage.Id + ".json");
        }
        return conversation;
    }
}