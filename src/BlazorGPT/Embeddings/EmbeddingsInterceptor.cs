using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using Microsoft.Extensions.Options;
using OpenAI.GPT3.Interfaces;

namespace BlazorGPT.Embeddings
{
    public class EmbeddingsInterceptor : IInterceptor
    {
        private RedisEmbeddings _redisEmbeddings;
        private IOpenAIService _aiService;
        private PipelineOptions _options;

        public string Name { get; } = "Embeddings";
        public bool Internal { get; } = false;


        public EmbeddingsInterceptor(IOptions<PipelineOptions> options, RedisEmbeddings redisEmbeddings, IOpenAIService aiService)
        {
            _options = options.Value;
            _aiService = aiService;
            _redisEmbeddings = redisEmbeddings;
        }

        public async Task<Conversation> Receive(Conversation conversation)
        {


            return conversation;
        }

        public async Task<Conversation> Send(Conversation conversation)
        {


            return conversation;
        }
    }
}
