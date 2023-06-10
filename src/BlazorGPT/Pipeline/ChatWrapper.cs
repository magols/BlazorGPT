using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
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


    public async Task<Conversation> SendWithPipeline(IKernel kernel,
        Conversation conversation ,
        IEnumerable<QuickProfile>? quickProfiles = null, 
        IEnumerable<IInterceptor>? enabledInterceptors = null)
    {
        var profiles = quickProfiles?.ToArray() ?? Array.Empty<QuickProfile>();
        var interceptors = enabledInterceptors?.ToArray() ?? Array.Empty<IInterceptor>();
        
        conversation = await _quickProfileHandler.Send(kernel, conversation, profiles.Where(p => p.InsertAt == InsertAt.Before));

        if (interceptors.Any())    
        {
            conversation = await _interceptorHandler.Send(kernel, conversation, interceptors);
        }
        
        conversation = await Send(kernel, conversation, profiles);

        conversation = await _quickProfileHandler.Receive(kernel, this, conversation, profiles.Where(p => p.InsertAt == InsertAt.After));

        if (interceptors.Any())
        {
            conversation = await _interceptorHandler.Receive(kernel, conversation, interceptors);
        }

        return  conversation;

    }


    private IQuickProfileHandler _quickProfileHandler;


    public Func<string, Task<string>> OnStreamCompletion = async (string s) =>
    {
        return s;
    };

    private IOptions<PipelineOptions> _pipelineOptions;
    private KernelService _kernelService;

    public async Task<Conversation> Send(IKernel kernel, 
        Conversation conversation, 
        IEnumerable<QuickProfile> profiles)
    {

        ChatHistory chatHistory = new ChatHistory();
        foreach (var message in conversation.Messages)
        {
            var role =
                message.Role == "system" ? ChatHistory.AuthorRoles.System : // if the role is system, set the role to system
                message.Role == "user" ? ChatHistory.AuthorRoles.User : ChatHistory.AuthorRoles.Assistant;

            chatHistory.AddMessage(role, message.Content);
        }


        var kernelStream = _kernelService.ChatCompletionAsStreamAsync(kernel, chatHistory, ChatHistory.AuthorRoles.User);

        var conversationMessage = new ConversationMessage(new ChatMessage("assistant", ""));
        conversation.AddMessage(conversationMessage);
        await foreach (var completion in kernelStream)
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


        await using var ctx = await _contextFactory.CreateDbContextAsync();


            if (conversation.Id == null || conversation.Id == default(Guid))
            {
         
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
            }

            if (!string.IsNullOrEmpty(conversation.UserId))
                await ctx.SaveChangesAsync();


            return conversation;



    }

    public async Task<Conversation> Send(IKernel kernel, Conversation conversation)
    {
        return await Send(kernel, conversation, Array.Empty<QuickProfile>());
    }
}