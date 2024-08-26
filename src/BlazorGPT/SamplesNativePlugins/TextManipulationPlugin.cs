using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace Samples.Native;

public class TextManipulationPlugin
{

    public TextManipulationPlugin(IServiceProvider serviceProvider)
    {
    }

    [KernelFunction("StringReverse")]
    [Description("Reverses a given input string and returns it")]
    public Task<string> ReverseString(
        [Description("String to reverse")]
        string input
    )
    {
         return Task.FromResult(new string(input.Reverse().ToArray()));
    }
}

