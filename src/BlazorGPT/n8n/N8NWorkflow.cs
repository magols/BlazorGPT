using System.ComponentModel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace n8n;

[Description("n8n")]
public class N8NWorkflow(IServiceProvider serviceProvider)
{

    string routerWorkflowId = "5f945e81-8609-4dc8-888e-9f26c63486f1";

    N8NClient n8NClient => serviceProvider.GetRequiredService<N8NClient>();

    [KernelFunction("n8n_workflow")]
    [Description("Calls an n8n workflow")]
    public async Task<string> Execute([Description("The name of the workflow to call")] string workflowName, [Description("Input from the user")] string input, object? metaData)
    {
      
        WorkflowRequest<string> request = new()
        {
            RouterWorkflow = routerWorkflowId,
            SubWorkflow = workflowName,
            Input = input,
            MetaData = metaData

        };

        var response = await n8NClient.ExecuteJson<string, string>(request);

        return response?.Result ?? "Error";
    }

    [KernelFunction("x_post")]
    [Description("Posts a tweet on Twitter/X")]
    public async Task<string> PostToX([Description("The content of the post")] string input)
    {
        WorkflowRequest<string> request = new()
        {
            RouterWorkflow = routerWorkflowId,
            SubWorkflow = "2tVEUK5S1eGLWtEp",
            Input = input

        };

        var response = await n8NClient.ExecuteJson<string, string>(request);

        return response?.Result ?? "Error";
    }

}
