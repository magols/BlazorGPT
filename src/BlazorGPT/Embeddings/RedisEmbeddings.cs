using StackExchange.Redis;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.Options;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using IDatabase = StackExchange.Redis.IDatabase;

namespace BlazorGPT.Embeddings
{


    public class RedisEmbeddings
    {
        private IDatabase? _db;
        private readonly PipelineOptions _options;
        private KernelService _kernelService;


        public RedisEmbeddings(IOptions<PipelineOptions> options, KernelService kernelService)
        {
            _kernelService = kernelService;
            _options = options.Value;

        }

        private void Connect()
        {
            if (_db == null)
            {
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.Embeddings.RedisConfigurationString);
                _db = redis.GetDatabase();
            }
        }

        public async Task RemoveIndexAsync(string indexName)
        {
            try
            {
                Connect();
                await _db.FT().DropIndexAsync(indexName);
            }
            catch (Exception)
            {
                
            }
        }

        public async Task<EmbeddingEntry> GetEmbedding(string dataKey)
        {
            Connect();
            var hash = await _db.HashGetAllAsync(dataKey);

            EmbeddingEntry e = new EmbeddingEntry();
            e.Id = dataKey;

            float[] vector = new float[1536];
            Buffer.BlockCopy(hash.First(h => h.Name == "embedding").Value, 0, vector, 0, 1536 * sizeof(float));
            e.Embedding = vector;

            e.Data = hash.First(h => h.Name == "data").Value.ToString().Replace("\\n", " ");
            return e;
        }


        public async Task<ICollection<Document>> Search(string indexName, ICollection<float> vec, int? limit = 5)
        {
            Connect();
            var res = await _db.FT().SearchAsync(indexName,
                new Query($"*=>[KNN {limit} @embedding $query_vec]")

                    .AddParam("query_vec", vec.SelectMany(BitConverter.GetBytes).ToArray())
                    .ReturnFields("__embedding_score")
                    .SetSortBy("__embedding_score")
                    .Dialect(2)
            );


            return res.Documents;
        }
        
        public async Task SaveEmbedding(string id, float[] vector, string data)
        {
            Connect();

            await _db.HashSetAsync(id, "data", data);
            await _db.HashSetAsync(id, "embedding", vector.SelectMany(BitConverter.GetBytes).ToArray());
        }

        public async Task CreateIndexAsync(string indexName)
        {
            Connect();

            _db.FT().Create(indexName,
                new FTCreateParams().On(IndexDataType.HASH)
                    .Prefix("auto-gpt:", "blazor-gpt:", "c:")
                    ,
                new Schema()
                    .AddVectorField("embedding", Schema.VectorField.VectorAlgo.FLAT,
                        new Dictionary<string, object>()
                        {
                            ["TYPE"] = "FLOAT32",
                            ["DIM"] = 1536,
                            ["DISTANCE_METRIC"] = "COSINE"
                        }
                    ));
        }
    }
}
