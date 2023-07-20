using System.ComponentModel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;

namespace BlazorGPT.Web.Plugins;

public class EmbeddingSkill
{
    [SKFunction]
    [Description("Includes the memory embeddings given a search query input")]
    [SKParameter("input", "Memory search query")]
    [SKParameter("collection", "The collection of memory to search in")]
    [SKParameter("relevance", "Only include results with relevance higher than")]
    [SKParameter("limit", "Amount of search results to include")]
    public async Task<string> Include(SKContext ctx)
    {
        var functionDefinition = @"
	        [EMBEDDINGS]
	        {{ memory.recall $input }}
	        [/EMBEDDINGS]
	        ";

        var promptRenderer = new PromptTemplateEngine();
        var renderedPrompt = await promptRenderer.RenderAsync(functionDefinition, ctx);
        return renderedPrompt;
    }
}