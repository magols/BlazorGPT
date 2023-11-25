using Microsoft.SemanticKernel.AI;

namespace BlazorGPT.Pipeline;

public class ChatRequestSettings : AIRequestSettings
{
    public ChatRequestSettings()
    {
        ExtensionData = new Dictionary<string, object>
        {
            { "MaxTokens", 2500 },
            { "Temperature", 0.0 },
            { "TopP", 0 },
            { "FrequencyPenalty", 0.0 },
            { "PresencePenalty", 0.0 },
            { "StopSequences", new[] { "Dragons be here" } }
        };
    }
}