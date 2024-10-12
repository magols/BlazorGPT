using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.Options;

namespace n8n;

public class WorkflowRequest<TData>
{

    public string SubWorkflow { get; set; }
    public TData Input { get; set; }

    // ignore json attribute if null
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? MetaData { get; set; }

    public string RouterWorkflow { get; set; }
}
 

public class WorkflowResponse<T>
{
    public string? Result { get; set; }
    public T? Data { get; set; }
    public WorkflowError? Error { get; set; }
}


public class WorkflowError
{
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; }
}

public class N8NClient(
    IHttpClientFactory httpClientFactory,
    IOptions<PipelineOptions> pipelineOptions)
{
    PipelineOptions Options => pipelineOptions.Value;

    readonly string _baseUrl = "http://localhost:5678";


    //public async Task<WorkflowResponse<TOutput>> RunWorkflow<TOutput, TInput>(TInput payload, bool test = false) where TInput : WorkflowRequest<TInput>
    //{
    //    return await ExecuteJson<TInput, TOutput>(payload, test);
    //}



    public async Task<WorkflowResponse<TOutput>> ExecuteJson<TInput, TOutput>(TInput payload, bool test = false)
        where TInput : WorkflowRequest<TInput>
    {
        var client = httpClientFactory.CreateClient("n8n");
        var url = $"{_baseUrl}/webhook{(test ? "-test" : "")}/{payload.RouterWorkflow}";

        HttpResponseMessage? response = null;

        response = await client.PostAsJsonAsync(url, payload);

        if (response is { IsSuccessStatusCode: true })
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObj = new WorkflowResponse<TOutput>();

            if (typeof(TOutput) == typeof(string))
            {
                responseObj.Result = responseContent;
            }
            else
            {
                responseObj.Data = JsonSerializer.Deserialize<TOutput>(responseContent);
            }

            return responseObj;
        }
        else
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var error = new WorkflowError
            {
                StatusCode = (int)response.StatusCode,
                ErrorMessage = responseContent
            };
            var responseObj = new WorkflowResponse<TOutput>
            {
                Error = error
            };
            return responseObj;

        }
    }

    public async Task<WorkflowResponse<Tout>> ExecuteJson<T, Tout>(WorkflowRequest<T> request)
    {

        var client = httpClientFactory.CreateClient("n8n");
        var url = $"{_baseUrl}/webhook/{request.RouterWorkflow}";
        var response = await client.PostAsJsonAsync(url, request);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (typeof(Tout) == typeof(string))
            {
                return new WorkflowResponse<Tout>
                {
                    Result = responseContent
                };
            }

            var responseObj = new WorkflowResponse<Tout>
            {
                Data = JsonSerializer.Deserialize<Tout>(responseContent)
            };
            return responseObj;
        }
        else
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var error = new WorkflowError
            {
                StatusCode = (int)response.StatusCode,
                ErrorMessage = responseContent
            };
            var responseObj = new WorkflowResponse<Tout>
            {
                Error = error
            };

            return responseObj;
        }
    }

}

 