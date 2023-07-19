namespace BlazorGPT.Pipeline;

public class PipelineOptions
{
    public string Model { get; set; }
    public string ModelEmbeddings { get; set; }
    public string ModelTextCompletions { get; set; }
    public string[] Models { get; set; }
    public string[]? EnabledInterceptors { get; set; }
    public string[]? PreSelectedInterceptors { get; set; }

    public string KrokiHost { get; set; }
    public string StateFileSaveInterceptorPath { get; set; }

    public EmbeddingsSettings Embeddings { get; set; } = new EmbeddingsSettings();

    public string BING_API_KEY { get; set; }
    public string GOOGLE_API_KEY { get; set; }
    public string GOOGLE_SEARCH_ENGINE_ID { get; set; }

}

public class EmbeddingsSettings
{
    public string RedisConfigurationString { get; set; }
    public string RedisIndexName { get; set; }
    public int MaxTokensToIncludeAsContext { get; set; }
    public bool UseRedis { get; set; }
    public bool UseSqlite { get; set; }
    public string SqliteConnectionString { get; set; }
}