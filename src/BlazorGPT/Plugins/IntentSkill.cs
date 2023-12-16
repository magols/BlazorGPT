using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Plugins;

public class IntentSkill
{
    private Kernel _kernel;

    public IntentSkill(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction("DetectIntent")]
    [Description("Given a query and a list of possible intents, detect which intent the input matches")]
    public async Task<string> DetectIntentAsync(
        [Description("Input to detect intent of")]
        string input
        )
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

        var function = _kernel.CreateFunctionFromPrompt(funcDesc, new PromptExecutionSettings() { });
        var result = await _kernel.InvokeAsync(function, new() { ["input"] = input });

        return result.GetValue<string>();
    }
}

