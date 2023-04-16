namespace BlazorGPT.Managers;

public class PipelineOptions
{
    public string Model { get; set; }
    public string[]? EnabledInterceptors { get; set; }
    public string KrokiHost { get; set; }
}