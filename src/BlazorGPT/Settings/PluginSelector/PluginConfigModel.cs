namespace BlazorGPT.Settings.PluginSelector
{
    public class SemanticPluginConfig
    {
        public int? schema { get; set; } = null;
        public string? type { get; set; } = null;
        public string? description { get; set; } = null;
        public CompletionConfig? completion { get; set; } = null;
        public InputConfig? input { get; set; } = null;
        public object[]? default_backends { get; set; } = null;
    }

    public class CompletionConfig
    {
        public int? max_tokens { get; set; } = null;
        public decimal temperature { get; set; } = 0;
        public decimal top_p { get; set; } = 0;
        public decimal presence_penalty { get; set; } = 0;
        public decimal frequency_penalty { get; set; } = 0;
    }

    public class InputConfig
    {
        public ParameterConfig[]? parameters { get; set; } = null;
    }

    public class ParameterConfig
    {
        public string? name { get; set; } = null;
        public string? description { get; set; } = null;
        public string? defaultValue { get; set; } = null;
    }

}
