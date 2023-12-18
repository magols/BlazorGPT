using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Redis;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;
using StackExchange.Redis;
#pragma warning disable SKEXP0003
#pragma warning disable SKEXP0011
#pragma warning disable SKEXP0052
#pragma warning disable SKEXP0012

#pragma warning disable SKEXP0027
#pragma warning disable SKEXP0028
namespace BlazorGPT.Pipeline;

public class KernelService
{
    private readonly PipelineOptions _options;

    public KernelService(IOptions<PipelineOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Kernel> CreateKernelAsync(string? model = null)
    {
        var useAzureOpenAI = _options.ServiceType != "OpenAI";

        var builder = Kernel.CreateBuilder();
        if (useAzureOpenAI)
        {
#pragma warning disable SKEXP0011
            builder
            .AddAzureOpenAIChatCompletion(model ?? _options.Model, _options.Endpoint, _options.ApiKey,
                    _options.ApiKey)
                .AddAzureOpenAITextEmbeddingGeneration(_options.ModelEmbeddings, _options.Endpoint, _options.ApiKey);
#pragma warning restore SKEXP0011

        }
        else
        {
#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            builder
                .AddOpenAIChatCompletion(model ?? _options.Model, _options.ApiKey, _options.OrgId)
                .AddOpenAITextEmbeddingGeneration(_options.ModelEmbeddings, _options.ApiKey, _options.OrgId)
                .AddOpenAITextToImage(_options.ApiKey, _options.OrgId);
#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        return builder.Build();
    }


    public async Task<ISemanticTextMemory> GetMemoryStore()
    {
        IMemoryStore memoryStore = null!;
        if (_options.Embeddings.UseSqlite)
            memoryStore = await SqliteMemoryStore.ConnectAsync(_options.Embeddings.SqliteConnectionString);
        if (_options.Embeddings.UseRedis)
        {
            var redis = ConnectionMultiplexer.Connect(_options.Embeddings.RedisConfigurationString);
            var _db = redis.GetDatabase();
            memoryStore = new RedisMemoryStore(_db);
        }

        var useAzureOpenAI = _options.ServiceType != "OpenAI";
        if (useAzureOpenAI)
        {
#pragma warning disable SKEXP0011
#pragma warning disable SKEXP0052


            var mem = new MemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(
                 _options.ModelEmbeddings,
                 _options.Endpoint,
                 _options.ApiKey,
                 _options.ModelEmbeddings
            )
            .WithMemoryStore(memoryStore)
            .Build();

            return mem;
        }
        else
        {
            var mem = new MemoryBuilder()
                .WithOpenAITextEmbeddingGeneration(modelId:_options.ModelEmbeddings, _options.ApiKey)
                .WithMemoryStore(memoryStore)
                .Build();
            return mem;
        }
    }

    public async Task<Conversation> ChatCompletionAsStreamAsync(Kernel kernel,
        Conversation conversation,
        PromptExecutionSettings requestSettings,
        Func<string, Task<string>> OnStreamCompletion,
        CancellationToken cancellationToken)
    {
        var chatHistory = new ChatHistory();
        foreach (var message in conversation.Messages.Where(c => !string.IsNullOrEmpty(c.Content.Trim())))
        {
            var role =
                message.Role == "system"
                    ? AuthorRole.System
                    : // if the role is system, set the role to system
                    message.Role == "user"
                        ? AuthorRole.User
                        : AuthorRole.Assistant;

            chatHistory.AddMessage(role, message.Content);
        }

        return await ChatCompletionAsStreamAsync(kernel, chatHistory, conversation, requestSettings, OnStreamCompletion,
            cancellationToken);
    }

    private async Task<Conversation> ChatCompletionAsStreamAsync(Kernel kernel,
        ChatHistory chatHistory,
        Conversation conversation,
        PromptExecutionSettings requestSettings,
        Func<string, Task<string>> onStreamCompletion, CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        var fullMessage = string.Empty;

        await foreach (var completionResult in chatCompletion.GetStreamingChatMessageContentsAsync(chatHistory,
                           requestSettings, cancellationToken: cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return conversation;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return conversation;
            }
 
            fullMessage += completionResult.Content;
            if (onStreamCompletion != null) await onStreamCompletion.Invoke(completionResult.Content);
 
        }

        conversation.Messages.Last().Content = fullMessage;
        return conversation;
    }
}