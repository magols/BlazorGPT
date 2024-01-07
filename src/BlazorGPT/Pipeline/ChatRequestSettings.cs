using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public class ChatRequestSettings : PromptExecutionSettings
{
    public ChatRequestSettings()
    {
        ExtensionData = new Dictionary<string, object>
        {
            
        };
    }
}