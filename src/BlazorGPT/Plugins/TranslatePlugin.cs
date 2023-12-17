using System.ComponentModel;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace BlazorGPT.Plugins;

public class TranslatePlugin
{
  
    private IServiceProvider _serviceProvider;

    public TranslatePlugin(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
         
    }

    [KernelFunction("Translate")]
    public async Task<string> TranslateAsync(
        [Description("Text to translate")]
        string input,
        [Description("Language to translate text into")]
        string language)
    {


        var funcDesc = """
            {{$input}}
            ---
            Translate that into {{ $language }}:

            """;

        var kernelService = _serviceProvider.GetRequiredService<KernelService>();
        var kernel = await kernelService.CreateKernelAsync();


        string promptTemplate = @"
Generate a creative reason or excuse for the given event.
Be creative and be funny. Let your imagination run wild.

Event: I am running late.
Excuse: I was being held ransom by giraffe gangsters.

Event: I haven't been to the gym for a year
Excuse: I've been too busy training my pet dragon.

Event: {{$input}}
";

        var excuseFunction = kernel.CreateFunctionFromPrompt(promptTemplate, new OpenAIPromptExecutionSettings() { MaxTokens = 100, Temperature = 0.4, TopP = 1 });

        var result = await kernel.InvokeAsync(excuseFunction, new() { ["input"] = "I missed the F1 final race" });
        Console.WriteLine(result.GetValue<string>());

        result = await kernel.InvokeAsync(excuseFunction, new() { ["input"] = "sorry I forgot your birthday" });
        Console.WriteLine(result.GetValue<string>());

        var fixedFunction = kernel.CreateFunctionFromPrompt($"Translate this date {DateTimeOffset.Now:f} to French format", new OpenAIPromptExecutionSettings() { MaxTokens = 100 });

        result = await kernel.InvokeAsync(fixedFunction);
        Console.WriteLine(result.GetValue<string>());



        var function = kernel.CreateFunctionFromPrompt(funcDesc, new PromptExecutionSettings() {});

        var result2 = await kernel.InvokeAsync(function, new() { ["input"] = input });

        return result2.GetValue<string>();
    }
}