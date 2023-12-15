using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
#pragma warning disable SKEXP0003

namespace BlazorGPT.Plugins;

public class EmbeddingSkill
{
    private Kernel _kernel;
    private ISemanticTextMemory _semanticTextMemory;

    public EmbeddingSkill(Kernel kernel, ISemanticTextMemory semanticTextMemory)
    {
        _semanticTextMemory = semanticTextMemory;
        _kernel = kernel;
    }

    [KernelFunction("IncludeEmbeddingsTag")]
    [Description("Includes the memory embeddings data enclosed in a tag, given a search query input")]

    public async Task<string> IncludeEmbeddingsTagAsync(
            [Description("Memory search query")]
            string input,
            [Description("The collection of memory to search in")]
            string collection = "blazorgpt",
            [Description( "Only include results with relevance higher than")]
            double relevance = 0.8d,
            [Description("Amount of search results to include" )]
            int limit = 2
      )
    {
        var res = _semanticTextMemory.SearchAsync(collection, input, limit, relevance);

        var fullText = "";
        await foreach (var r in res) fullText += r.Metadata.Text + " ";

        if (string.IsNullOrEmpty(fullText)) return "";

        return $"[EMBEDDINGS]{fullText}[/EMBEDDINGS]";
    }
} 