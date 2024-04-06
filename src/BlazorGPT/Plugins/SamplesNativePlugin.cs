using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Plugins;

public class SamplesNativePlugin
{

    public SamplesNativePlugin(IServiceProvider serviceProvider)
    {
    }

    [KernelFunction("StringReverse")]
    [Description("Reverses a given input string and returns it")]
    public async Task<string> ReverseString(
        [Description("String to reverse")]
        string input
    )
    {
         return new string(input.Reverse().ToArray());
    }
}