using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Memory.Sqlite;

namespace BlazorGPT.Pipeline
{
    public class KernelService
    {
        //private readonly IKernel _kernel;
        //private readonly IChatCompletion _chatGPT;
        readonly KernelSettings _kernelSettings;
        public KernelService()
        {
             _kernelSettings = KernelSettings.LoadSettings();
        }

        public async Task<IKernel> CreateKernelAsync()
        {
            bool useAzureOpenAI = _kernelSettings.ServiceType != "OpenAI";

            var builder = new KernelBuilder();
            if (useAzureOpenAI)
            {
                builder
                    .WithAzureChatCompletionService(_kernelSettings.ChatDeploymentOrModelId, _kernelSettings.Endpoint, _kernelSettings.ApiKey)
                    .WithAzureTextCompletionService(_kernelSettings.ChatDeploymentOrModelId, _kernelSettings.Endpoint, _kernelSettings.ApiKey)
                    .WithAzureTextEmbeddingGenerationService(_kernelSettings.ChatDeploymentOrModelId, _kernelSettings.Endpoint, _kernelSettings.ApiKey);
            }
            else
            {
                builder
                    .WithOpenAIChatCompletionService(_kernelSettings.ChatDeploymentOrModelId, _kernelSettings.ApiKey, _kernelSettings.OrgId)
                    .WithOpenAITextCompletionService(_kernelSettings.TextDeploymentOrModelId, _kernelSettings.ApiKey, _kernelSettings.OrgId)
                    .WithOpenAITextEmbeddingGenerationService(_kernelSettings.EmbeddingsDeploymentOrModelId, _kernelSettings.ApiKey, _kernelSettings.OrgId)
                    .WithOpenAIImageGenerationService(_kernelSettings.ApiKey, _kernelSettings.OrgId);
            }

            return builder
                .WithMemoryStorage(await SqliteMemoryStore.ConnectAsync(@"c:\temp\memory.sqlite"))
                .Build();
        }


        public Func<string, Task<string>> OnChatStreamCompletion = async (string s) => s;

        public async IAsyncEnumerable<string> ChatCompletionAsStreamAsync( ChatHistory chatHistory,
            ChatHistory.AuthorRoles authorRole = ChatHistory.AuthorRoles.Assistant)
        {
            var kernel = await CreateKernelAsync();
            var chatCompletion = kernel.GetService<IChatCompletion>();

            string fullMessage = string.Empty;

            await foreach (string message in chatCompletion.GenerateMessageStreamAsync(chatHistory))
            {
                fullMessage += message;
                if (OnChatStreamCompletion != null)
                {
                    await OnChatStreamCompletion.Invoke(message);
                }

                yield return message;
            }

            chatHistory.AddMessage(authorRole, fullMessage);
        }


        public async Task<string> SaveInformationAsync(string collection, string text, string id, string? description = null)
        {
            var kernel = await CreateKernelAsync();
         
            var newId = await kernel.Memory.SaveInformationAsync(collection, text, id, description);
            return newId;
        }

        public async Task<string> SaveReferenceAsync(string collection, string text, string id, string externalSourceName, string? description = null)
        {
            var kernel = await CreateKernelAsync();
            var newId = await kernel.Memory.SaveReferenceAsync(collection, text, id, externalSourceName, description);
            return newId;
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


//
//internal static class KernelConfigExtensions
//{
//	/// <summary>
//	/// Adds a text completion service to the list. It can be either an OpenAI or Azure OpenAI backend service.
//	/// </summary>
//	/// <param name="kernelConfig"></param>
//	/// <param name="kernelSettings"></param>
//	/// <exception cref="ArgumentException"></exception>
//	internal static void AddCompletionBackend(this KernelConfig kernelConfig, KernelSettings kernelSettings)
//	{
//		switch (kernelSettings.ServiceType.ToUpperInvariant())
//		{
//			case KernelSettings.AzureOpenAI:
//				kernelConfig.AddAzureTextCompletionService(deploymentName: kernelSettings.DeploymentOrModelId, endpoint: kernelSettings.Endpoint, apiKey: kernelSettings.ApiKey, serviceId: kernelSettings.ServiceId);
//				break;
//
//			case KernelSettings.OpenAI:
//				kernelConfig.AddOpenAITextCompletionService(modelId: kernelSettings.DeploymentOrModelId, apiKey: kernelSettings.ApiKey, orgId: kernelSettings.OrgId, serviceId: kernelSettings.ServiceId);
//				break;
//
//			default:
//				throw new ArgumentException($"Invalid service type value: {kernelSettings.ServiceType}");
//		}
//	}
//}


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

            Console.WriteLine($"Semantic kernel settings '{DefaultConfigFile}' not found, attempting to load configuration from user secrets.");

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