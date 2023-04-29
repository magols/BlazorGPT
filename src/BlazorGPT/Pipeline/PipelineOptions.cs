namespace BlazorGPT.Pipeline;

public class PipelineOptions
{
    public string Model { get; set; } = "gpt-3.5-turbo";
    public string[] Models { get; set; }
    public string[]? EnabledInterceptors { get; set; }
    public string KrokiHost { get; set; }
    public string StateFileSaveInterceptorPath { get; set; }
}