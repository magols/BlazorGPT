using System.Text;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

namespace BlazorGPT.Embeddings
{
    public class EmbeddingsInterceptor : IInterceptor
    {
        private readonly RedisEmbeddings _redisEmbeddings;
        private readonly PipelineOptions _options;
        private readonly string IndexName = "blazorgpt_idx";
        private KernelService _kernelService;

        public string Name { get; } = "Embeddings";
        public bool Internal { get; } = false;


        public EmbeddingsInterceptor(IOptions<PipelineOptions> options, RedisEmbeddings redisEmbeddings, KernelService kernelService)
        {
            _kernelService = kernelService;
            _options = options.Value;
            _redisEmbeddings = redisEmbeddings;

            if (!string.IsNullOrEmpty(_options.Embeddings.RedisIndexName))
            {
                IndexName = _options.Embeddings.RedisIndexName;
            }
        }

        public async Task<Conversation> Receive(IKernel kernel, Conversation conversation)
        {
            return conversation;
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation)
        {

            if (conversation.Messages.Count == 2)
            {
                var prompt = conversation.Messages.First(m => m.Role == "user");
                var promptEmbed = await CreatePromptEmbedding(prompt.Content);
                // search for embeddings in redis 

                var docs = await _redisEmbeddings.Search(IndexName, promptEmbed.Embedding, 30);

                StringBuilder contextBuilder = new StringBuilder();
                contextBuilder.Append("[EMBEDDINGS]");

                // make a counter, loop while counter < 1500
                int maxEmbeddingsTokens = _options.Embeddings.MaxTokensToIncludeAsContext;
                int currentEmbeddingsTokens = 0;


                foreach (var doc in docs)
                {
                    var embeddingDoc = await _redisEmbeddings.GetEmbedding(doc.Id);
                    currentEmbeddingsTokens += GPT3Tokenizer.Encode(embeddingDoc.Data).Count;
                    if (currentEmbeddingsTokens > maxEmbeddingsTokens)
                        break;
                    contextBuilder.Append(embeddingDoc.Data + " ");
                }

                contextBuilder.Append("[/EMBEDDINGS] ");
                prompt.Content = contextBuilder + prompt.Content;
            }

            return conversation;
        }

        private async Task<EmbeddingEntry> CreatePromptEmbedding(string userPrompt)
        {

            var kernel = await _kernelService.CreateKernelAsync();
            var _aiService = kernel.GetService<ITextCompletion>();
            var embeddingGeneration = kernel.GetService<ITextEmbeddingGeneration>();

            var result = await embeddingGeneration.GenerateEmbeddingsAsync(new List<string>() { userPrompt });
 
                var embedding = result.First();
                return new EmbeddingEntry()
                {
                    Id = userPrompt,
                    Embedding = embedding.Vector.Select(e => e).ToArray()
                };
            

        }
    }
}


