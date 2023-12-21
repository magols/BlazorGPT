using System.Globalization;
using System.Linq;
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

    public async Task<Kernel> CreateKernelAsync()
    {
        return await CreateKernelAsync(null, null);
    }

    public async Task<Kernel> CreateKernelAsync(string model)
    {
        return await CreateKernelAsync(null, model);
    }


    public async Task<Kernel> CreateKernelAsync(ChatModelsProvider? provider, 
        string? model = null)
    {
        if (model == "") model = null;
        var builder = Kernel.CreateBuilder();

        if (provider == null)
        {
            if (_options.Providers.OpenAI.IsConfigured())
            {
                provider = ChatModelsProvider.OpenAI;
            }
            else if (_options.Providers.AzureOpenAI.IsConfigured())
            {
                provider = ChatModelsProvider.AzureOpenAI;
            }
            else if (_options.Providers.Local.IsConfigured())
            {
                provider = ChatModelsProvider.Local;
            }

            if (provider == null)
            {
                throw new InvalidOperationException("No model provider is configured");
            }
        }

        if (provider == ChatModelsProvider.AzureOpenAI)
        {
            model ??= _options.Providers.AzureOpenAI.ChatModel;

#pragma warning disable SKEXP0011
            builder
            .AddAzureOpenAIChatCompletion(
                deploymentName: _options.Providers.AzureOpenAI.ChatModels.First( p => p.Key == model).Key,
                modelId: model,
                endpoint: _options.Providers.AzureOpenAI.Endpoint,
                apiKey: _options.Providers.AzureOpenAI.ApiKey
                )
            .AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: _options.Providers.AzureOpenAI.EmbeddingsModels.First(p => p.Key == _options.Providers.AzureOpenAI.EmbeddingsModel).Key,
                modelId: _options.Providers.AzureOpenAI.EmbeddingsModel,
                endpoint: _options.Providers.AzureOpenAI.Endpoint,
                apiKey: _options.Providers.AzureOpenAI.ApiKey
                );
#pragma warning restore SKEXP0011
        }
        if (provider == ChatModelsProvider.OpenAI)

        {
            model ??= _options.Providers.OpenAI.ChatModel;
#pragma warning disable SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            builder
                .AddOpenAIChatCompletion(model, _options.Providers.OpenAI.ApiKey)
                .AddOpenAITextEmbeddingGeneration(_options.Providers.OpenAI.EmbeddingsModel, _options.Providers.OpenAI.ApiKey)
                .AddOpenAITextToImage(_options.Providers.OpenAI.ApiKey);

#pragma warning restore SKEXP0011 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        }

        return builder.Build();
    }

    public async Task<ISemanticTextMemory> GetMemoryStore()
    {
        return await GetMemoryStore(null);
    }


    public async Task<ISemanticTextMemory> GetMemoryStore(EmbeddingsModelProvider? provider)
    {
        if (provider == null)
        {
            if (_options.Providers.OpenAI.IsConfigured())
            {
                provider = EmbeddingsModelProvider.OpenAI;
            }
            else if (_options.Providers.AzureOpenAI.IsConfigured())
            {
                provider = EmbeddingsModelProvider.AzureOpenAI;
            }
            else if (_options.Providers.Local.IsConfigured())
            {
                provider = EmbeddingsModelProvider.Local;
            }

            if (provider == null)
            {
                throw new InvalidOperationException("No embeddings model provider is configured");
            }
        }


        IMemoryStore memoryStore = null!;
        if (_options.Embeddings.UseSqlite)
            memoryStore = await SqliteMemoryStore.ConnectAsync(_options.Embeddings.SqliteConnectionString);
        if (_options.Embeddings.UseRedis)
        {
            var redis = ConnectionMultiplexer.Connect(_options.Embeddings.RedisConfigurationString);
            var _db = redis.GetDatabase();
            memoryStore = new RedisMemoryStore(_db);
        }

        if (provider == EmbeddingsModelProvider.AzureOpenAI)
        {
#pragma warning disable SKEXP0011
#pragma warning disable SKEXP0052

            var mem = new MemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(
                deploymentName: _options.Providers.AzureOpenAI.EmbeddingsModels.First(o => o.Value == _options.Providers.AzureOpenAI.EmbeddingsModel).Key,
                modelId: _options.Providers.AzureOpenAI.EmbeddingsModel,
                endpoint: _options.Providers.AzureOpenAI.Endpoint,
                apiKey: _options.Providers.AzureOpenAI.ApiKey
            )
            .WithMemoryStore(memoryStore)
            .Build();

            return mem;
        }
        
        if (provider == EmbeddingsModelProvider.OpenAI)
        {
            var mem = new MemoryBuilder()
                .WithOpenAITextEmbeddingGeneration(modelId:_options.Providers.OpenAI.EmbeddingsModel, _options.Providers.OpenAI.ApiKey)
                .WithMemoryStore(memoryStore)
                .Build();
            return mem;
        }

        // todo: add local embeddings
        throw new InvalidOperationException("No embeddings provider is configured");

        return new MemoryBuilder()
            .WithMemoryStore(memoryStore)
            .Build();
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