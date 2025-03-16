using System.Text.Json.Serialization;
 

namespace BlazorGPT.Pipeline;

public class ModelsProvidersOptions
{
    public OpenAIModelsOptions OpenAI { get; set; } = new OpenAIModelsOptions();
    public AzureOpenAIModelsOptions AzureOpenAI { get; set; } = new AzureOpenAIModelsOptions();
    public GoogleAIOptions GoogleAI { get; set; } = new GoogleAIOptions();

    public OllamaOptions Ollama { get; set; } = new OllamaOptions();

    public ChatModelsProvider GetChatModelsProvider()
    {
        if (OpenAI.IsConfigured())
        {
            return ChatModelsProvider.OpenAI;
        }
        else
        {
            if (AzureOpenAI.IsConfigured())
            {
                return ChatModelsProvider.AzureOpenAI;
            }
            if (GoogleAI.IsConfigured())
            {
                return ChatModelsProvider.GoogleAI;
            }
        }

        return ChatModelsProvider.GoogleAI; 
    }

    public string GetChatModel()
    {

        if (OpenAI.IsConfigured())
        {
            return OpenAI.ChatModel;
        }
        else
        {
            if (AzureOpenAI.IsConfigured())
            {
                return AzureOpenAI.ChatModel;
            }

            if (GoogleAI.IsConfigured())
            {
                return GoogleAI.ChatModel;
            }
        }

        return string.Empty;
    }

    public string GetEmbeddingsModel()
    {
        if (OpenAI.IsConfigured())
        {
            return OpenAI.EmbeddingsModel;
        }

        if (AzureOpenAI.IsConfigured())
        {
            return AzureOpenAI.EmbeddingsModel;
        }

        if (Ollama.IsConfigured())
        {
            return Ollama.EmbeddingsModel;
        }

        if (GoogleAI.IsConfigured())
        {
            return GoogleAI.EmbeddingsModel;
        }

        return string.Empty;
    }

    public EmbeddingsModelProvider GetEmbeddingsModelProvider()
    {

        if (OpenAI.IsConfigured())
        {
            return EmbeddingsModelProvider.OpenAI;
        }

        if (AzureOpenAI.IsConfigured())
        {
            return EmbeddingsModelProvider.AzureOpenAI;
        }

        if (Ollama.IsConfigured())
        {
            return EmbeddingsModelProvider.Ollama;
        }

        if (GoogleAI.IsConfigured())
        {
            return EmbeddingsModelProvider.GoogleAI;
        }

        throw new Exception("No embeddings model provider configured");

    }
}


public class OllamaOptions
{
	public string BaseUrl { get; set; } = "";
    public string[] Models { get; set; } = Array.Empty<string>();
    public string ChatModel { get; set; } = default!;

    public string[] EmbeddingsModels { get; set; } = Array.Empty<string>();
    public string EmbeddingsModel { get; set; } = default!;

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(BaseUrl);
    }
}


public enum ChatModelsProvider
{
    OpenAI,
    AzureOpenAI,
    Ollama,
    GoogleAI,
}

public enum EmbeddingsModelProvider
{
    OpenAI,
    AzureOpenAI,
    Ollama,
    GoogleAI
}


public class OpenAIModelsOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;
    public string[] ChatModels { get; set; } = Array.Empty<string>();

    public string EmbeddingsModel { get; set; } = string.Empty;
    public string[] EmbeddingsModels { get; set; } = Array.Empty<string>();

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(ApiKey);
    }
}

// options for GoogleAI Cloud AI models
public class GoogleAIOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;
    public string[] ChatModels { get; set; } = Array.Empty<string>();

    public string EmbeddingsModel { get; set; } = string.Empty;
    public string[] EmbeddingsModels { get; set; } = Array.Empty<string>();

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(ApiKey);
    }
}

public class AzureOpenAIModelsOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;

    public string ChatModel { get; set; } = string.Empty;
    public Dictionary<string, string> ChatModels { get; set; } = new Dictionary<string, string>();

    public string EmbeddingsModel { get; set; } = string.Empty;


    // deploymentId, modelId
    public Dictionary<string,string> EmbeddingsModels { get; set; } = new Dictionary<string,string>();

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(ApiKey);
    }
}

public class PipelineOptions
{
    public ModelsProvidersOptions Providers { get; set; } =  new ModelsProvidersOptions();

    public int MaxTokens { get; set; }

    public float Temperature { get; set; }
    public float TopP { get; set; }
    public float PresencePenalty { get; set; }
    public float FrequencyPenalty { get; set; }
 
    public int MaxPlannerTokens { get; set; }

    public string[]? EnabledInterceptors { get; set; }
    public string[]? PreSelectedInterceptors { get; set; }

    public string? KrokiHost { get; set; } = default!;
    public string? StateFileSaveInterceptorPath { get; set; } = default!;

    public EmbeddingsSettings Embeddings { get; set; } = new EmbeddingsSettings();

    public string? BING_API_KEY { get; set; } = default!;
    public string? GOOGLE_API_KEY { get; set; } = default!;
    public string? GOOGLE_SEARCH_ENGINE_ID { get; set; } = default!;

    public Bot Bot { get; set; } = new Bot();

    public MemoryOptions Memory { get; set; } = new MemoryOptions();

}

public class MemoryOptions
{
    public bool Enabled { get; set; }
    public string Url { get; set; } = default!;
    public string ApiKey { get; set; } = default!;

    public string AzureStorageConnectionString { get; set; } = default!;
    // redis
    public string RedisConnectionString { get; set; } = default!;

}

public class Bot
{
    public string BotSystemInstruction { get; set; } = default!;
    public string BotUserId { get; set; } = default!;
}

public class EmbeddingsSettings
{
    public string RedisConfigurationString { get; set; } = default!;
    public string RedisIndexName { get; set; } = default!;
    public int MaxTokensToIncludeAsContext { get; set; } = default!;
    public bool UseRedis { get; set; }
    public bool UseSqlite { get; set; }
    public string SqliteConnectionString { get; set; } = default!;
}
