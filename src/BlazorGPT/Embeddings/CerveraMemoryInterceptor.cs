using System.Text;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

namespace BlazorGPT.Embeddings
{
    public class CerveraMemoryInterceptor : IInterceptor
    {
        private readonly PipelineOptions _options;

        public string Name { get; } = "SK CervMemory";
        public bool Internal { get; } = false;

        public CerveraMemoryInterceptor(IOptions<PipelineOptions> options)
        {
            _options = options.Value;
        }

        public async Task<Conversation> Receive(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {
            return conversation;
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {

            if (conversation.Messages.Count == 2)
            {
                var prompt = conversation.Messages.First(m => m.Role == "user");
                var searchResults = kernel.Memory.SearchAsync("cervera", prompt.Content, limit: 3, minRelevanceScore: 0.8, withEmbeddings: false);
                
                StringBuilder contextBuilder = new StringBuilder();
                contextBuilder.Append("[EMBEDDINGS]");

                // fill embeddings to the given token window limit
                int maxEmbeddingsTokens = _options.Embeddings.MaxTokensToIncludeAsContext;
                int currentEmbeddingsTokens = 0;
                await foreach (var res in searchResults)
                {
                    currentEmbeddingsTokens += GPT3Tokenizer.Encode(res.Metadata.Text).Count;
                    if (currentEmbeddingsTokens > maxEmbeddingsTokens)
                        break;
                    contextBuilder.Append($"rel: {res.Relevance}");
                    contextBuilder.Append(res.Metadata.Text + " ");
                }

                contextBuilder.Append("[/EMBEDDINGS] ");
                prompt.Content = contextBuilder + prompt.Content;
            }

            return conversation;
        }
    }
}


