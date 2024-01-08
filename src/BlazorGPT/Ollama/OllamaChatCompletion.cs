using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace BlazorGPT.Ollama
{
    public class OllamaChatCompletion(
    string modelId,
    string baseUrl,
    HttpClient http,
    ILoggerFactory? loggerFactory)
    : OllamaBase<OllamaChatCompletionService>(modelId, baseUrl, http, loggerFactory), IChatCompletionService
    {
        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null, CancellationToken cancellationToken = new())
        {
            var system = string.Join("\n", chatHistory.Where(x => x.Role == AuthorRole.System).Select(x => x.Content));
            
            var prompt = string.Join("\n", chatHistory.Where(x => x.Role != AuthorRole.System).Select(x => x.Role + ": " + x.Content));

            var data = new
            {
                model = Attributes["model_id"] as string,
                prompt = prompt,
                system,
                stream = false,
                options = executionSettings?.ExtensionData,
            };

            var response = await Http.PostAsJsonAsync($"{Attributes["base_url"]}/api/generate", data, cancellationToken).ConfigureAwait(false);

            ValidateOllamaResponse(response);

            var json = JsonSerializer.Deserialize<JsonNode>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            return new List<ChatMessageContent> { new(AuthorRole.Assistant, json!["response"]!.GetValue<string>(), modelId: Attributes["model_id"] as string) };
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null, Kernel? kernel = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = new())
        {

            throw new NotImplementedException("At this point in time, Ollama does not support streaming in SK ");
            yield return null;
        }

    }
}
