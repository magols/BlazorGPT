using System.ComponentModel;
using Microsoft.SemanticKernel;


namespace BlazorGPT.Plugins;

public class TranslateSkill
{
    private Kernel _kernel;

    public TranslateSkill(Kernel kernel)
    {
        _kernel = kernel;
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


        var function = _kernel.CreateFunctionFromPrompt(funcDesc, new PromptExecutionSettings() {});

        var result = await _kernel.InvokeAsync(function, new() { ["input"] = input });

        return result.GetValue<string>();
    }
}