using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;

namespace BlazorGPT.Plugins;

public class EmbeddingSkill
{
    private IKernel _kernel;

    public EmbeddingSkill(IKernel kernel)
    {
        _kernel = kernel;
    }

    [SKFunction, SKName("IncludeEmbeddingsTag")]
    [Description("Includes the memory embeddings data enclosed in a tag, given a search query input")]
    [SKParameter("input", "Memory search query")]
    [SKParameter("collection", "The collection of memory to search in", DefaultValue = "memory")]
    [SKParameter("relevance", "Only include results with relevance higher than", DefaultValue = "0.8")]
    [SKParameter("limit", "Amount of search results to include", DefaultValue = "2")]
    public async Task<string> IncludeEmbeddingsTagAsync(SKContext ctx)
    {
        var ts = new TextMemorySkill(memory: _kernel.Memory);
        double.TryParse(ctx.Variables["relevance"], out var relevance);
        int.TryParse(ctx.Variables["limit"], out var limit);

        var res = _kernel.Memory.SearchAsync(ctx.Variables["collection"], ctx.Variables["input"], limit, relevance);

        var fullText = "";
        await foreach (var r in res) fullText += r.Metadata.Text + " ";

        if (string.IsNullOrEmpty(fullText)) return "";

        var functionDefinition = $"[EMBEDDINGS]{fullText}[/EMBEDDINGS]";
        var promptRenderer = new PromptTemplateEngine();
        var renderedPrompt = await promptRenderer.RenderAsync(functionDefinition, ctx);
        return renderedPrompt;
    }
}