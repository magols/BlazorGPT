using BlazorGPT.Data;
using BlazorGPT.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace BlazorGPT.Managers;

public class ChatWrapper
{
    // This is the wrapper class that will be used to call the pipeline
    // it shall have the same methods as the pipeline
    private readonly IInterceptorHandler _interceptorHandler;
    private readonly IOpenAIService _openAiService;
    private readonly IDbContextFactory<BlazorGptDBContext> _contextFactory;

    public ChatWrapper(IInterceptorHandler interceptorHandler, 
        IQuickProfileHandler quickProfileHandler,
        IOpenAIService openAiService, 
        IDbContextFactory<BlazorGptDBContext> contextFactory, IOptions<PipelineOptions> pipelineOptions)
    {
        _pipelineOptions = pipelineOptions;
        _quickProfileHandler = quickProfileHandler;
        _contextFactory = contextFactory;
        _openAiService = openAiService;
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

    public async Task<Conversation> Send(Conversation conversation, IEnumerable<QuickProfile> profiles)
    {

            var stream = _openAiService.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest()
            {
                Model = _pipelineOptions.Value.Model,
                MaxTokens = 2000,
                Temperature = 0.3f,
                Messages = conversation.Messages.Select(m => new ChatMessage(m.Role, m.Content)).ToList()

            });

            var conversationMessage = new ConversationMessage(new ChatMessage("assistant", ""));
            // add the result, with cost
            conversation.AddMessage(conversationMessage);

            await foreach (var completion in stream)
            {
                if (completion.Successful)
                {
                    string? content = completion.Choices.First()?.Message.Content;
                    if (content != null)
                    {
                        conversationMessage.Content += content;
                        if (OnStreamCompletion!= null)
                        {
                          await  OnStreamCompletion.Invoke(content);   
                        }
                        
                        await Task.Delay(50); // adjust the delay time as needed
                    }
                }
                else
                {
                    if (completion.Error == null)
                    {
                        throw new Exception("Unknown Error");
                    }
                    Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                }
            }


            // save stuff
     //       StateHasChanged();

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