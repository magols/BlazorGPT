namespace BlazorGPT.Pipeline;

public class PipelineOptions
{
    public string? Model { get; set; }
    public string[] Models { get; set; }
    public string[]? EnabledInterceptors { get; set; }
    public string KrokiHost { get; set; }
    public string StateFileSaveInterceptorPath { get; set; }
}