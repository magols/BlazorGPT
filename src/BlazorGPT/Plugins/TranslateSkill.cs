using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.TemplateEngine.Basic;


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
    public async Task<string> TranslateAsync(
        [Description("Text to translate")]
        string input,
        [Description("Language to translate text into")]
        string language,
        SKContext ctx)
    {


        var funcDesc = """
            {{$input}}
            ---
            Translate that into {{ $language }}:

            """;



        var promptRenderer = new BasicPromptTemplateEngine();
        var prompt = await promptRenderer.RenderAsync(funcDesc, ctx);

        var func = _kernel.CreateSemanticFunction(funcDesc);
        var res = await func.InvokeAsync(ctx);
        return res.ToString();
    }
}