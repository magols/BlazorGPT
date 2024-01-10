using System.ComponentModel;
using BlazorGPT.Pipeline;
using BlazorGPT.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0003

namespace BlazorGPT.Plugins;

public class EmbeddingsPlugin
{
    private readonly IServiceProvider _serviceProvider;
    private ModelConfigurationService _modelConfigurationService;


    public EmbeddingsPlugin(IServiceProvider serviceProvider)
    {
        _modelConfigurationService = serviceProvider.GetService<ModelConfigurationService>()!;

        _serviceProvider = serviceProvider;
    }


    [KernelFunction("IncludeEmbeddingsTag")]
    [Description("Includes the memory embeddings data enclosed in a tag, given a search query input")]
    public async Task<string> IncludeEmbeddingsTagAsync(
        [Description("Memory search query")] string input,
        [Description("The collection of memory to search in")]
        string collection = "blazorgpt",
        [Description("Only include results with relevance higher than")]
        double relevance = 0.75d,
        [Description("Amount of search results to include")]
        int limit = 10
    )
    {
        ModelConfiguration model = _modelConfigurationService.GetDefaultConfig();

        var kernelService =
            _serviceProvider.GetRequiredService<KernelService>();

        var memStore = await kernelService.GetMemoryStore(model.EmbeddingsProvider, model.EmbeddingsModel);
        var res = memStore.SearchAsync(collection, input, limit, relevance).ConfigureAwait(true);

        var fullText = "";
        await foreach (var r in res) fullText += r.Metadata.Text + " ";

        if (string.IsNullOrEmpty(fullText)) return "";

        return $"[EMBEDDINGS]{fullText}[/EMBEDDINGS]";
    }
}