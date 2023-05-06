namespace BlazorGPT.Embeddings;

public class EmbeddingEntry
{
    public string Id { get; set; }
    public string Data { get; set; }
    public float[] Embedding { get; set; }
}