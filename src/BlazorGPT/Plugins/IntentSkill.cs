using System.ComponentModel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;

namespace BlazorGPT.Plugins;

public class IntentSkill
{
    [SKFunction, SKName("DetectIntent")]
    [Description("Given a query and a list of possible intents, detect which intent the input matches")]
    [SKParameter("input", "Input to detect intent of")]
    public async Task<string> DetectIntentAsync(SKContext ctx)
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

        var promptRenderer = new PromptTemplateEngine();
        var prompt = await promptRenderer.RenderAsync(funcDesc, ctx);
        return prompt;
    }
}

