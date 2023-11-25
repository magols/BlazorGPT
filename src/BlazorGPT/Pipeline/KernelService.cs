using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.Memory.Redis;
using Microsoft.SemanticKernel.Connectors.Memory.Sqlite;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using StackExchange.Redis;

namespace BlazorGPT.Pipeline;

public class KernelService
{
    private readonly PipelineOptions _options;

    public KernelService(IOptions<PipelineOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IKernel> CreateKernelAsync(string? model = null)
    {
        var useAzureOpenAI = _options.ServiceType != "OpenAI";

        var builder = new KernelBuilder();
        if (useAzureOpenAI)
            builder
                .WithAzureOpenAIChatCompletionService(model ?? _options.Model, _options.Endpoint, _options.ApiKey)
                .WithAzureOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _options.Endpoint,
                    _options.ApiKey);
        else
            builder
                .WithOpenAIChatCompletionService(model ?? _options.Model, _options.ApiKey, _options.OrgId)
                .WithOpenAITextCompletionService(_options.ModelTextCompletions, _options.ApiKey, _options.OrgId)
                .WithOpenAIImageGenerationService(_options.ApiKey, _options.OrgId);

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
            var mem = new MemoryBuilder()
                .WithAzureOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _options.Endpoint,
                    _options.ApiKey)
                .WithMemoryStore(memoryStore)
                .Build();
            return mem;
        }
        else
        {
            var mem = new MemoryBuilder()
                .WithOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _options.ApiKey)
                .WithMemoryStore(memoryStore)
                .Build();
            return mem;
        }
    }

    public async Task<Conversation> ChatCompletionAsStreamAsync(IKernel kernel,
        Conversation conversation,
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

        return await ChatCompletionAsStreamAsync(kernel, chatHistory, conversation, OnStreamCompletion,
            cancellationToken);
    }

    private async Task<Conversation> ChatCompletionAsStreamAsync(IKernel kernel,
        ChatHistory chatHistory,
        Conversation conversation,
        Func<string, Task<string>> onStreamCompletion, CancellationToken cancellationToken)
    {
        var chatCompletion = kernel.GetService<IChatCompletion>();
        var fullMessage = string.Empty;

        // todo: get chat request parameters from settings
        var chatRequestSettings = new AIRequestSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                { "MaxTokens", 2500 },
                { "Temperature", 0.0 },
                { "TopP", 1 },
                { "FrequencyPenalty", 0.0 },
                { "PresencePenalty", 0.0 },
                { "StopSequences", new[] { "Dragons be here" } }
            }
        };


        List<IAsyncEnumerable<string>> resultTasks = new();
        var currentResult = 0;

        await foreach (var completionResult in chatCompletion.GetStreamingChatCompletionsAsync(chatHistory,
                           chatRequestSettings, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return conversation;
                throw new TaskCanceledException();
            }

            var message = string.Empty;

            await foreach (var chatMessage in completionResult.GetStreamingChatMessageAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return conversation;
                    throw new TaskCanceledException();
                }

                var role = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(chatMessage.Role.Label);
                message += chatMessage.Content;
                fullMessage += chatMessage.Content;
                if (onStreamCompletion != null) await onStreamCompletion.Invoke(chatMessage.Content);
            }
        }

        conversation.Messages.Last().Content = fullMessage;
        return conversation;
    }
}