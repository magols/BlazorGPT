//using System.ComponentModel;
//using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Orchestration;
//using Microsoft.SemanticKernel.TemplateEngine.Basic;

//namespace BlazorGPT.Web.Plugins;

//public class EmbeddingSkill
//{

//    [SKFunction, Description("Includes the memory embeddings given a search query input")]
//    public async Task<string> Include(
//        SKContext ctx,
//        [Description("Memory search query")]
//        string input,
//        [Description("The collection of memory to search in")]
//        string collection = "memory",
//        [Description( "Only include results with relevance higher than")]
//        double relevance = 0.8d,
//        [Description("Amount of search results to include" )]
//        int limit = 2)
//    {
//        string functionDefinition = @"
//	        [EMBEDDINGS]
//	        {{ memory.recall $input }}
//	        [/EMBEDDINGS]
//	        ";

//        var promptRenderer = new BasicPromptTemplateEngine();
//        var renderedPrompt = await promptRenderer.RenderAsync(functionDefinition, ctx);
//        return renderedPrompt;
//    }
 
//}