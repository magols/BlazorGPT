using System.Text;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace BlazorGPT.Embeddings
{
    public class EmbeddingsInterceptor : IInterceptor
    {
        private readonly RedisEmbeddings _redisEmbeddings;
        private readonly IOpenAIService _aiService;
        private readonly PipelineOptions _options;
        private readonly string IndexName = "blazorgpt_idx";

        public string Name { get; } = "Embeddings";
        public bool Internal { get; } = false;


        public EmbeddingsInterceptor(IOptions<PipelineOptions> options, RedisEmbeddings redisEmbeddings, IOpenAIService aiService)
        {
            _options = options.Value;
            _aiService = aiService;
            _redisEmbeddings = redisEmbeddings;

            if (!string.IsNullOrEmpty( _options.Embeddings.RedisIndexName))
            {
                IndexName = _options.Embeddings.RedisIndexName;
            }
        }

        public async Task<Conversation> Receive(Conversation conversation)
        {
            return conversation;
        }

        public async Task<Conversation> Send(Conversation conversation)
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
                    currentEmbeddingsTokens += OpenAI.GPT3.Tokenizer.GPT3.TokenizerGpt3.TokenCount(embeddingDoc.Data);
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
            var embeddingResult = await _aiService.Embeddings.CreateEmbedding(new EmbeddingCreateRequest()
            {
                Model = Models.TextEmbeddingAdaV2,
                Input = userPrompt
            });

            if (embeddingResult.Successful)
            {
                var embedding = embeddingResult.Data.First();
                return new EmbeddingEntry()
                {
                    Id = userPrompt,
                    Embedding = embedding.Embedding.Select(e => (float)e).ToArray()
                };
            }

            throw new Exception("Failed to create embedding");
        }
    }
}


