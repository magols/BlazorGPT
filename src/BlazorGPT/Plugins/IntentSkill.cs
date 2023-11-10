using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.TemplateEngine.Basic;

namespace BlazorGPT.Plugins;

public class IntentSkill
{
    [SKFunction, SKName("DetectIntent")]
    [Description("Given a query and a list of possible intents, detect which intent the input matches")]
    public async Task<string> DetectIntentAsync(
        [Description("Input to detect intent of")]
        string input,
        SKContext ctx)
    {
        var funcDesc = """
            These are available intents that one might query:

            GetPinballMachineFact
            TellMeMore,
            WeatherForecast

            Which intent is this query asking for? If none match, respond with Unknown.

            {{$input}}

            Intent:
            """;

        var promptRenderer = new BasicPromptTemplateEngine();
        var prompt = await promptRenderer.RenderAsync(funcDesc, ctx);
        return prompt;
    }
}

