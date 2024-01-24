using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
 

namespace BlazorGPT.Pipeline;

public class ChatWrapper
{
    private readonly IDbContextFactory<BlazorGptDBContext> _contextFactory;

    // This is the wrapper class that will be used to call the pipeline
    // it shall have the same methods as the pipeline
    private readonly IInterceptorHandler _interceptorHandler;
    private readonly KernelService _kernelService;

    private IOptions<PipelineOptions> _pipelineOptions;


    private readonly IQuickProfileHandler _quickProfileHandler;


    public Func<string, Task<string>> OnStreamCompletion = async s => { return s; };

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


    public async Task<Conversation> SendWithPipeline(Kernel kernel,
        Conversation conversation,
        ChatRequestSettings? requestSettings = default,
        Func<string, Task<string>>? callback = null,
        IEnumerable<QuickProfile>? quickProfiles = null,
        IEnumerable<IInterceptor>? enabledInterceptors = null, 
        CancellationToken cancellationToken = default)
    {
        var profiles = quickProfiles?.ToArray() ?? Array.Empty<QuickProfile>();
        var interceptors = enabledInterceptors?.ToArray() ?? Array.Empty<IInterceptor>();

        conversation =
            await _quickProfileHandler.Send(kernel, conversation, profiles.Where(p => p.InsertAt == InsertAt.Before));

        if (interceptors.Any())
            conversation = await _interceptorHandler.Send(kernel, conversation, enabledInterceptors: interceptors, cancellationToken: cancellationToken);

        conversation = await Send(kernel, requestSettings, conversation, profiles, callback, cancellationToken: cancellationToken);

        conversation = await _quickProfileHandler.Receive(kernel, this, conversation,
            profiles.Where(p => p.InsertAt == InsertAt.After));

        if (interceptors.Any())
            conversation = await _interceptorHandler.Receive(kernel, conversation, interceptors, cancellationToken: cancellationToken);

        return conversation;
    }

    public async Task<Conversation> Send(Kernel kernel,
        PromptExecutionSettings requestSettings,
        Conversation conversation,
        IEnumerable<QuickProfile> profiles, Func<string, Task<string>>? callback = null,
        CancellationToken cancellationToken = default)
    {
        if (conversation.StopRequested) return conversation;

        
        var conversationMessage = new ConversationMessage("assistant", "");
        conversation.AddMessage(conversationMessage);
        await _kernelService.ChatCompletionAsStreamAsync(kernel, conversation, requestSettings: requestSettings, callback, 
            cancellationToken: cancellationToken);

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);


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

    public async Task<Conversation> Send(Kernel kernel, PromptExecutionSettings requestSettings, Conversation conversation)
    {
        return await Send(kernel, requestSettings, conversation, Array.Empty<QuickProfile>());
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation)
    {

        return await Send(kernel,  new ChatRequestSettings(), conversation, Array.Empty<QuickProfile>());
    }
}