﻿using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Memory.Redis;
using Microsoft.SemanticKernel.Connectors.Memory.Sqlite;
using StackExchange.Redis;
using System.Globalization;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BlazorGPT.Pipeline
{
    public class KernelService
    {
        private KernelSettings? _kernelSettings;

        public KernelService(IOptions<PipelineOptions> options)
        {
            _options = options.Value;
             
        }

        public async Task<IKernel> CreateKernelAsync(string? model = null)
        {
            _kernelSettings = KernelSettings.LoadSettings();
            bool useAzureOpenAI = _kernelSettings.ServiceType != "OpenAI";

            var builder = new KernelBuilder();
            if (useAzureOpenAI)
            {
                builder
                    .WithAzureOpenAIChatCompletionService(model ?? _options.Model, _kernelSettings.Endpoint, _kernelSettings.ApiKey)
                    .WithAzureOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _kernelSettings.Endpoint, _kernelSettings.ApiKey);
            }
            else
            {
                builder
                    .WithOpenAIChatCompletionService(model ?? _options.Model, _kernelSettings.ApiKey, _kernelSettings.OrgId)
                    .WithOpenAITextCompletionService(_options.ModelTextCompletions, _kernelSettings.ApiKey, _kernelSettings.OrgId)
                    .WithOpenAIImageGenerationService(_kernelSettings.ApiKey, _kernelSettings.OrgId);
            }

            return builder.Build();
        }


        public async Task<ISemanticTextMemory> GetMemoryStore()
        {
            _kernelSettings = KernelSettings.LoadSettings();

            IMemoryStore memoryStore = null!;
            if (_options.Embeddings.UseSqlite)
            {
                memoryStore = await SqliteMemoryStore.ConnectAsync(_options.Embeddings.SqliteConnectionString);
            }
            if (_options.Embeddings.UseRedis)
            {
                ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(_options.Embeddings.RedisConfigurationString);
                var _db = redis.GetDatabase();
                memoryStore = new RedisMemoryStore(_db, 1536);
            }

            bool useAzureOpenAI = _kernelSettings.ServiceType != "OpenAI";
            if (useAzureOpenAI)
            {

                var mem = new MemoryBuilder()
                    .WithAzureOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _kernelSettings.Endpoint, _kernelSettings.ApiKey)
                    .WithMemoryStore(memoryStore)
                    .Build();
                return mem;
            }
            else
            {
                var mem = new MemoryBuilder()
                    .WithOpenAITextEmbeddingGenerationService(_options.ModelEmbeddings, _kernelSettings.ApiKey)
                    .WithMemoryStore(memoryStore)
                    .Build();
                return mem;
            }

        }

        private readonly PipelineOptions _options;

        public async Task<Conversation> ChatCompletionAsStreamAsync(IKernel kernel,
            Conversation conversation,
            Func<string, Task<string>> OnStreamCompletion, 
            CancellationToken cancellationToken)
        {
            ChatHistory chatHistory = new ChatHistory();
            foreach (var message in conversation.Messages.Where(c => !string.IsNullOrEmpty(c.Content.Trim())))
            {
                var role =
                    message.Role == "system"
                        ?
                        AuthorRole.System
                        : // if the role is system, set the role to system
                        message.Role == "user"
                            ? AuthorRole.User
                            : AuthorRole.Assistant;

                chatHistory.AddMessage(role, message.Content);
            }

            return await ChatCompletionAsStreamAsync(kernel, chatHistory, conversation, OnStreamCompletion, cancellationToken);
        }

        private async Task<Conversation> ChatCompletionAsStreamAsync(IKernel kernel, 
            ChatHistory chatHistory,
            Conversation conversation, 
            Func<string, Task<string>> onStreamCompletion, CancellationToken cancellationToken)
        {
            var chatCompletion = kernel.GetService<IChatCompletion>();
            string fullMessage = string.Empty;

            // todo: get chat request parameters from settings
            var chatRequestSettings = new AIRequestSettings
            {
                ExtensionData = new Dictionary<string, object>()
                {
                    { "MaxTokens", 2500 },
                    { "Temperature", 0.0 },
                    { "TopP", 1 },
                    { "FrequencyPenalty", 0.0 },
                    { "PresencePenalty", 0.0 },
                    { "StopSequences", new[] { "Dragons be here" } }    
                }
            };


            List<IAsyncEnumerable<string>> resultTasks = new();
            int currentResult = 0;

            await foreach (var completionResult in chatCompletion.GetStreamingChatCompletionsAsync(chatHistory, chatRequestSettings, cancellationToken))
            {
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return conversation;
                    throw new TaskCanceledException();
                }

                string message = string.Empty;

                await foreach (var chatMessage in completionResult.GetStreamingChatMessageAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return conversation;
                        throw new TaskCanceledException();
                    }

                    string role = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(chatMessage.Role.Label);
                    message += chatMessage.Content;
                    fullMessage += chatMessage.Content;
                    if (onStreamCompletion != null)
                    {
                        await onStreamCompletion.Invoke(chatMessage.Content);
                    }
                }
            }

            conversation.Messages.Last().Content = fullMessage;
            return conversation;
        }
    }
}






public static class Settings
{
    private const string DefaultConfigFile = "config/settings.json";
    private const string TypeKey = "type";
    private const string ModelKey = "model";
    private const string EndpointKey = "endpoint";
    private const string SecretKey = "apikey";
    private const string OrgKey = "org";
    private const bool StoreConfigOnFile = true;


    // Load settings from file
    public static (bool useAzureOpenAI, string model, string azureEndpoint, string apiKey, string orgId)
        LoadFromFile(string configFile = DefaultConfigFile)
    {
        if (!File.Exists(DefaultConfigFile))
        {
            Console.WriteLine("Configuration not found: " + DefaultConfigFile);
            Console.WriteLine("\nPlease run the Setup Notebook (0-AI-settings.ipynb) to configure your AI backend first.\n");
            throw new Exception("Configuration not found, please setup the notebooks first using notebook 0-AI-settings.pynb");
        }

        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(DefaultConfigFile));
            bool useAzureOpenAI = config[TypeKey] == "azure";
            string model = config[ModelKey];
            string azureEndpoint = config[EndpointKey];
            string apiKey = config[SecretKey];
            string orgId = config[OrgKey];
            if (orgId == "none") { orgId = ""; }

            return (useAzureOpenAI, model, azureEndpoint, apiKey, orgId);
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
            return (true, "", "", "", "");
        }
    }

    // Delete settings file
    public static void Reset(string configFile = DefaultConfigFile)
    {
        if (!File.Exists(configFile)) { return; }

        try
        {
            File.Delete(configFile);
            Console.WriteLine("Settings deleted. Run the notebook again to configure your AI backend.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }
    }

    // Read and return settings from file
    private static (bool useAzureOpenAI, string model, string azureEndpoint, string apiKey, string orgId)
        ReadSettings(bool _useAzureOpenAI, string configFile)
    {
        // Save the preference set in the notebook
        bool useAzureOpenAI = _useAzureOpenAI;
        string model = "";
        string azureEndpoint = "";
        string apiKey = "";
        string orgId = "";

        try
        {
            if (File.Exists(configFile))
            {
                (useAzureOpenAI, model, azureEndpoint, apiKey, orgId) = LoadFromFile(configFile);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }

        // If the preference in the notebook is different from the value on file, then reset
        if (useAzureOpenAI != _useAzureOpenAI)
        {
            Reset(configFile);
            useAzureOpenAI = _useAzureOpenAI;
            model = "";
            azureEndpoint = "";
            apiKey = "";
            orgId = "";
        }

        return (useAzureOpenAI, model, azureEndpoint, apiKey, orgId);
    }

    // Write settings to file
    private static void WriteSettings(
        string configFile, bool useAzureOpenAI, string model, string azureEndpoint, string apiKey, string orgId)
    {
        try
        {
            if (StoreConfigOnFile)
            {
                var data = new Dictionary<string, string>
                {
                    { TypeKey, useAzureOpenAI ? "azure" : "openai" },
                    { ModelKey, model },
                    { EndpointKey, azureEndpoint },
                    { SecretKey, apiKey },
                    { OrgKey, orgId },
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(configFile, JsonSerializer.Serialize(data, options));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }

        // If asked then delete the credentials stored on disk
        if (!StoreConfigOnFile && File.Exists(configFile))
        {
            try
            {
                File.Delete(configFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong: " + e.Message);
            }
        }
    }
}



public class KernelSettings
{
    public const string DefaultConfigFile = "config/appsettings.json";
    public const string OpenAI = "OPENAI";
    public const string AzureOpenAI = "AZUREOPENAI";

    [JsonPropertyName("serviceType")]
    public string ServiceType { get; set; } = string.Empty;

    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = string.Empty;

    [JsonPropertyName("chatDeploymentOrModelId")]
    public string ChatDeploymentOrModelId { get; set; } = string.Empty;
    [JsonPropertyName("textDeploymentOrModelId")]
    public string TextDeploymentOrModelId { get; set; } = string.Empty;
    [JsonPropertyName("embeddingsDeploymentOrModelId")]
    public string EmbeddingsDeploymentOrModelId { get; set; } = string.Empty;


    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("orgId")]
    public string OrgId { get; set; } = string.Empty;

    [JsonPropertyName("logLevel")]
    public LogLevel? LogLevel { get; set; }

    /// <summary>
    /// Load the kernel settings from settings.json if the file exists and if not attempt to use user secrets.
    /// </summary>
    internal static KernelSettings LoadSettings()
    {
        try
        {
            if (File.Exists(DefaultConfigFile))
            {
                return FromFile(DefaultConfigFile);
            }

//            Console.WriteLine($"Semantic kernel settings '{DefaultConfigFile}' not found, attempting to load configuration from user secrets.");

            return FromUserSecrets();
        }
        catch (InvalidDataException ide)
        {
            Console.Error.WriteLine(
                "Unable to load semantic kernel settings, please provide configuration settings using instructions in the README.\n" +
                "Please refer to: https://github.com/microsoft/semantic-kernel-starters/blob/main/sk-csharp-hello-world/README.md#configuring-the-starter"
            );
            throw new InvalidOperationException(ide.Message);
        }
    }

    /// <summary>
    /// Load the kernel settings from the specified configuration file if it exists.
    /// </summary>
    internal static KernelSettings FromFile(string configFile = DefaultConfigFile)
    {
        if (!File.Exists(configFile))
        {
            throw new FileNotFoundException($"Configuration not found: {configFile}");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile(configFile, optional: true, reloadOnChange: true)
            .Build();

        return configuration.Get<KernelSettings>()
               ?? throw new InvalidDataException($"Invalid semantic kernel settings in '{configFile}', please provide configuration settings using instructions in the README.");
    }

    /// <summary>
    /// Load the kernel settings from user secrets.
    /// </summary>
    internal static KernelSettings FromUserSecrets()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("aspnet-BlazorGPT.Web-c48be10f-d69c-4f07-a429-1e1b836ce01d")
            .AddUserSecrets<KernelSettings>()
            .Build();

        return configuration.Get<KernelSettings>()
               ?? throw new InvalidDataException("Invalid semantic kernel settings in user secrets, please provide configuration settings using instructions in the README.");
    }
}