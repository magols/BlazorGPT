using Microsoft.SemanticKernel;
using System.Text;

namespace BlazorGPT.Pipeline.Interceptors;

public class StructurizrDslInterceptor : InterceptorBase, IInterceptor, IStateWritingInterceptor
{
 
    private readonly ConversationsRepository _conversationsRepository;
    private readonly IDbContextFactory<BlazorGptDBContext> _context;

    public StructurizrDslInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        if (conversation.Messages.Count() == 2)
        {
            await AppendInstruction(conversation);
        }

        return conversation;
    }

    private async Task<Conversation> AppendInstruction(Conversation conversation)
    {
        string state = @"

        workspace {

            model {
                user = person ""User"" ""A user of my software system.""
                softwareSystem = softwareSystem ""Software System"" ""My software system.""

                user->softwareSystem ""Uses""
            }

            views {
                systemContext softwareSystem ""SystemContext"" {
                    include*
                    autoLayout
                }

                styles {
                    element ""Software System"" {
                        background #1168bd
                        color #ffffff
                    }
                    element ""Person"" {
                        shape person
                        background #08427b
                        color #ffffff
                    }
                }
            }

        }
        ";


        var contentBuilder = new StringBuilder();
        contentBuilder.Append("[INSTRUCTION]Here is the data for an initial DSL model. We will iteratively add data to this data on our respective side. If either one has changes the structure or values of the model we shall send over the model and data enclosed in a [STATEDATA][/STATEDATA] tag in our message. In the STATEDATA tag nothing else should appear.  We will send it to eachother ONLY when data has changed! ");
        contentBuilder.Append(" Here is the base data. Remember from the start not to send it back unless you have changed structure or values.");
        contentBuilder.Append($" [STATEDATA]{state}[/STATEDATA][/INSTRUCTION]");

        var outgoing = conversation.Messages.ElementAt(1).Content;
        contentBuilder.AppendLine(outgoing);
        conversation.Messages.ElementAt(1).Content = contentBuilder.ToString();

        conversation.Messages.ElementAt(1).State = new MessageState()
        {
            Type = "StructurizrDslInterceptor",
            Content = state,
            IsPublished = true,
            Name = "msgstate",
        };

        return conversation;
    }   

    private string path = @"C:\source\BlazorGPT\BlazorGPT\wwwroot\state\";


    public bool Internal { get; } = false;

    public async Task<Conversation> Receive(Kernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
    {
        var lastMsg = conversation.Messages.Last();
        await ParseMessageAndSaveState(lastMsg, "StructurizrDslInterceptor");

        if (File.Exists(path + lastMsg.Id))
        {
            File.Move(path + lastMsg.Id, path + "structurizr\\" + lastMsg.Id + ".dsl");
            File.Copy(
                path + "structurizr\\" + lastMsg.Id + ".dsl", path + "structurizr\\workspace.dsl", true);

        }

        return conversation;
    }


    public override string Name { get; } = "Structurizr Hive DSL";

    public static string DecodeStringFromCSharp(string input)
    {
        return input
            .Replace("\\\\", "\\")
            .Replace("\\\"", "\"")
            .Replace("\\\'", "\'")
            .Replace("\\n", "")
            .Replace("\\r", "");
    }

    public static string EscapeStringForCSharp(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\'", "\\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}