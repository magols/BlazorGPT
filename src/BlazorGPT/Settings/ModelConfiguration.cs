using BlazorGPT.Pipeline;

namespace BlazorGPT.Settings;

public class ModelConfiguration
{
    public ChatModelsProvider Provider { get; set; }
    public string Model { get; set; } = null!;
    public int MaxTokens { get; set; }
    public int MaxPlannerTokens { get; set; }
    public float Temperature { get; set; }

    public EmbeddingsModelProvider EmbeddingsProvider { get; set; }
    public string EmbeddingsModel { get; set; } = null!;


}