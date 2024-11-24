using BlazorGPT.Pipeline;

namespace BlazorGPT.Settings;

public class ModelConfiguration
{
    public ChatModelsProvider Provider { get; set; }
    public string Model { get; set; } = null!;
    public int MaxTokens { get; set; }
    public float Temperature { get; set; }

    public float TopP { get; set; }
    public float PresencePenalty { get; set; }
    public float FrequencyPenalty { get; set; }
    public IEnumerable<string> StopSequences { get; set; } = null!;


    public int MaxPlannerTokens { get; set; }


    public EmbeddingsModelProvider EmbeddingsProvider { get; set; }
    public string EmbeddingsModel { get; set; } = null!;


}