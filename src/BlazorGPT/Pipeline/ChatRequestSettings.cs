using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public class ChatRequestSettings : PromptExecutionSettings
{
    public ChatRequestSettings()
    {
        ExtensionData = new Dictionary<string, object>
        {
            { "MaxTokens", 4000 },
            { "Temperature", 0.0 },
            { "TopP", 0 },
            { "FrequencyPenalty", 0.0 },
            { "PresencePenalty", 0.0 },
            { "StopSequences", new[] { "Dragons be here" } }
        };
    }
}