using System.Text;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SharpToken;


namespace BlazorGPT.Embeddings;

public class EmbeddingsInterceptor : InterceptorBase, IInterceptor
{
    private readonly PipelineOptions _options;
    private readonly string IndexName = "blazorgpt";
    private readonly KernelService _kernelService;
    private ModelConfigurationService _modelConfigurationService;

    public EmbeddingsInterceptor(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _modelConfigurationService = serviceProvider.GetRequiredService<ModelConfigurationService>();
        _kernelService = serviceProvider.GetRequiredService<KernelService>();
        _options = serviceProvider.GetRequiredService<IOptions<PipelineOptions>>().Value;

        if (!string.IsNullOrEmpty(_options.Embeddings.RedisIndexName)) IndexName = _options.Embeddings.RedisIndexName;
    }

    public string Name { get; } = "Embeddings";
    public bool Internal { get; } = false;

    public async Task<Conversation> Receive(Kernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        return conversation;
    }

    public async Task<Conversation> Send(Kernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Messages.Count == 2)
        {
            var modelConfig = await _modelConfigurationService.GetConfig();

            var prompt = conversation.Messages.First(m => m.Role == "user");

            var memStore = await _kernelService.GetMemoryStore(modelConfig.EmbeddingsProvider, modelConfig.EmbeddingsModel);
            var searchResult = memStore.SearchAsync(IndexName, prompt.Content, 10, 0.75d, cancellationToken: cancellationToken);
            var maxTokens = _options.Embeddings.MaxTokensToIncludeAsContext;
            var tokens = 0;

            var sb = new StringBuilder();
            sb.Append("[EMBEDDINGS]");
            await foreach (var message in searchResult)
            {
                var encoding = GptEncoding.GetEncodingForModel("gpt-4");
                tokens += encoding.Encode(message.Metadata.Text).Count;
                if (tokens > maxTokens)
                    break;
                sb.Append(message.Metadata.Text + " ");
            }
            sb.Append("[/EMBEDDINGS] ");
            prompt.Content = sb + prompt.Content;

            return conversation;

        }

        return conversation;
    }
}