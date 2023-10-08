using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;

namespace BlazorGPT.Plugins;

public class TranslateSkill
{
    private IKernel _kernel;

    public TranslateSkill(IKernel kernel)
    {
        _kernel = kernel;
    }

    [SKFunction]
    [SKName("Translate")]
    [Description("Translates a text into a named langague")]
    [SKParameter("input", "Text to translate")]
    [SKParameter("language", "Language to translate text into")]
    public async Task<string> TranslateAsync(SKContext ctx)
    {


        var funcDesc = """
            {{$input}}
            ---
            Translate that into {{ $language }}:

            """;



        var promptRenderer = new PromptTemplateEngine();
        var prompt = await promptRenderer.RenderAsync(funcDesc, ctx);

        var func = _kernel.CreateSemanticFunction(funcDesc);
        var res = await func.InvokeAsync(ctx);
        return res.Result;
    }
}