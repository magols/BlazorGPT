using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace BlazorGPT.Ollama;

public class OllamaTextEmbeddingGenerationService(
    string modelId,
    string baseUrl,
    HttpClient http,
    ILoggerFactory? loggerFactory)
    : OllamaBase<OllamaTextEmbeddingGeneration>(modelId, baseUrl, http, loggerFactory),
        IEmbeddingGenerationService<string, float>, ITextEmbeddingGenerationService
#pragma warning restore SKEXP0001

{

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, Kernel? kernel = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var result = new List<ReadOnlyMemory<float>>(data.Count);

        foreach (var text in data)
        {
            var request = new
            {
                model = Attributes["model_id"],
                prompt = text
            };

            var response = await Http
                .PostAsJsonAsync($"{Attributes["base_url"]}/api/embeddings", request, cancellationToken)
                .ConfigureAwait(false);

            ValidateOllamaResponse(response);

            var json = JsonSerializer.Deserialize<JsonNode>(await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false));

            var embedding = new ReadOnlyMemory<float>(json!["embedding"]?.AsArray().GetValues<float>().ToArray());

            result.Add(embedding);
        }

        return result;
    }
}