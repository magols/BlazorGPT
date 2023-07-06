using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;

namespace BlazorGPT.Web.Plugins;

public class EmbeddingSkill
{

    [SKFunction("Includes the embeddings given a search query input")]
    [SKFunctionContextParameter(Name = "input", Description = "Memory search query")]
    [SKFunctionContextParameter(Name = "collection", Description = "The collection of memory to search in")]
    [SKFunctionContextParameter(Name = "relevance", Description = "Only include results with relevance higher than")]
    [SKFunctionContextParameter(Name = "limit", Description = "Amount of search results to include")]
    public async Task<string> Include(SKContext ctx)
    {
        string functionDefinition = @"
	        [EMBEDDINGS]
	        {{ memory.recall $input }}
	        [/EMBEDDINGS]
	        ";

        var promptRenderer = new PromptTemplateEngine();
        var renderedPrompt = await promptRenderer.RenderAsync(functionDefinition, ctx);
        return renderedPrompt;
    }
 
}