using System.Text;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SharpToken;


namespace BlazorGPT.Embeddings;

public class EmbeddingsInterceptor : IInterceptor
{
    private readonly PipelineOptions _options;
    private readonly string IndexName = "blazorgpt";
    private readonly KernelService _kernelService;


    public EmbeddingsInterceptor(IOptions<PipelineOptions> options,
        KernelService kernelService)
    {
        _kernelService = kernelService;
        _options = options.Value;

        if (!string.IsNullOrEmpty(_options.Embeddings.RedisIndexName)) IndexName = _options.Embeddings.RedisIndexName;
    }

    public string Name { get; } = "Embeddings";
    public bool Internal { get; } = false;

    public async Task<Conversation> Receive(IKernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        return conversation;
    }

    public async Task<Conversation> Send(IKernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Messages.Count == 2)
        {
            var prompt = conversation.Messages.First(m => m.Role == "user");

            var memStore = await _kernelService.GetMemoryStore();
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