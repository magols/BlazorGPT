using System.Text.Json.Serialization;

namespace BlazorGPT.Pipeline;

public class PipelineOptions
{
    public string ServiceType { get; set; } = string.Empty;

 
    public string Endpoint { get; set; } = string.Empty;

 
    public string ApiKey { get; set; } = string.Empty;

 
    public string OrgId { get; set; } = string.Empty;

    public string Model { get; set; }
    public string ModelEmbeddings { get; set; }
    public string ModelTextCompletions { get; set; }
    public string[] Models { get; set; }

    public int MaxTokens { get; set; }

    public string[]? EnabledInterceptors { get; set; }
    public string[]? PreSelectedInterceptors { get; set; }

    public string KrokiHost { get; set; }
    public string StateFileSaveInterceptorPath { get; set; }

    public EmbeddingsSettings Embeddings { get; set; } = new EmbeddingsSettings();

    public string BING_API_KEY { get; set; }
    public string GOOGLE_API_KEY { get; set; }
    public string GOOGLE_SEARCH_ENGINE_ID { get; set; }

    public FileUpload FileUpload { get; set; } = new FileUpload();

    public Bot Bot { get; set; } = new Bot();

}

public class Bot
{
    public string BotSystemInstruction { get; set; }
    public string BotUserId { get; set; }
    public FileUpload FileUpload { get; set; } = new FileUpload();

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

public class FileUpload
{
    public bool Enabled { get; set; }
}