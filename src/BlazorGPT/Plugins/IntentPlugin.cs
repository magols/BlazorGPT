using System.ComponentModel;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Plugins;

public class IntentPlugin
{
    private Kernel _kernel;
    private readonly IServiceProvider _serviceProvider;

    public IntentPlugin(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

        var kernelService = _serviceProvider.GetRequiredService<KernelService>();

        var k = await kernelService.CreateKernelAsync();
        var function = k.CreateFunctionFromPrompt(funcDesc, new PromptExecutionSettings());
        var result = await k.InvokeAsync(function, new KernelArguments { ["input"] = input });

        var retVal = result.GetValue<string>();
        return retVal;
    }
}