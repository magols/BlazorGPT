using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;

namespace BlazorGPT.Pipeline;

public class ChatWrapper
{
    // This is the wrapper class that will be used to call the pipeline
    // it shall have the same methods as the pipeline
    private readonly IInterceptorHandler _interceptorHandler;
    private readonly IDbContextFactory<BlazorGptDBContext> _contextFactory;

    public ChatWrapper(KernelService kernelService, IInterceptorHandler interceptorHandler, 
        IQuickProfileHandler quickProfileHandler,
        IDbContextFactory<BlazorGptDBContext> contextFactory, IOptions<PipelineOptions> pipelineOptions)
    {
        _kernelService = kernelService;
        _pipelineOptions = pipelineOptions;
        _quickProfileHandler = quickProfileHandler;
        _contextFactory = contextFactory;
        _interceptorHandler = interceptorHandler;
    }


    public async Task<Conversation> SendWithPipeline(Conversation conversation,
        IEnumerable<QuickProfile>? quickProfiles = null
   , IEnumerable<IInterceptor>? enabledInterceptors = null)
    {
        var profiles = quickProfiles?.ToArray() ?? Array.Empty<QuickProfile>();
        var interceptors = enabledInterceptors?.ToArray() ?? Array.Empty<IInterceptor>();
        
        conversation = await _quickProfileHandler.Send(conversation, profiles.Where(p => p.InsertAt == InsertAt.Before));

        if (interceptors.Any())    
        {
            conversation = await _interceptorHandler.Send(conversation, interceptors);
        }
        
        conversation = await Send(conversation, profiles);

        conversation = await _quickProfileHandler.Receive(this, conversation, profiles.Where(p => p.InsertAt == InsertAt.After));

        if (interceptors.Any())
        {
            conversation = await _interceptorHandler.Receive(conversation, interceptors);
        }

        return  conversation;

    }


    //public Func<Task<string>>? OnStreamCompletion = null;
    private IQuickProfileHandler _quickProfileHandler;


    // make an OnCompletion func that also takes string as a parameter
    public Func<string, Task<string>> OnStreamCompletion = async (string s) =>
    {
        return s;
    };

    private IOptions<PipelineOptions> _pipelineOptions;
    private KernelService _kernelService;

    public async Task<Conversation> Send(Conversation conversation, IEnumerable<QuickProfile> profiles)
    {

        ChatHistory chatHistory = new ChatHistory();
        foreach (var message in conversation.Messages)
        {
            var role =
                message.Role == "system" ? ChatHistory.AuthorRoles.System : // if the role is system, set the role to system
                message.Role == "user" ? ChatHistory.AuthorRoles.User : ChatHistory.AuthorRoles.Assistant;

            chatHistory.AddMessage(role, message.Content);
        }


        var kernelStream = _kernelService.ChatCompletionAsStreamAsync(chatHistory, ChatHistory.AuthorRoles.User);

        var conversationMessage = new ConversationMessage(new ChatMessage("assistant", ""));
        conversation.AddMessage(conversationMessage);
        await foreach (var completion in kernelStream)
        {
            if (true)
            {
                string? content ; // = completion.Choices.First()?.Message.Content;
                content = completion;
                if (content != null)
                {
                    conversationMessage.Content += content;
                    if (OnStreamCompletion != null)
                    {
                        await OnStreamCompletion.Invoke(content);
                    }
                    
                    await Task.Delay(50); // adjust the delay time as needed
                }
            }
            else
            {
                //if (completion.Error == null)
                //{
                //    throw new Exception("Unknown Error");
                //}
                //Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
            }
        }





        await using var ctx = await _contextFactory.CreateDbContextAsync();

        //    bool isNew = false;
         //   bool wasSummarized = false;

            if (conversation.Id == null || conversation.Id == default(Guid))
            {
           //     conversation.UserId = userId;
              //  isNew = true;

                foreach (var p in profiles.Where(p => p.Id != Guid.Empty))
                {
                    ctx.Attach(p);
                    conversation.QuickProfiles.Add(p);
                }

                ctx.Conversations.Add(conversation);
            }
            else
            {
                ctx.Attach(conversation);
            }


            if (conversation.Summary == null)
            {
                var last = conversation.Messages.Last().Content;
                conversation.Summary =
                    last.Substring(0, last.Length >= 75 ? 75 : last.Length);
          //      wasSummarized = true;
            }

            if (!string.IsNullOrEmpty(conversation.UserId))
                await ctx.SaveChangesAsync();


            return conversation;



    }

    public async Task<Conversation> Send(Conversation conversation)
    {
        return await Send(conversation, Array.Empty<QuickProfile>());
    }
}