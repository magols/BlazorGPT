using System.ComponentModel;
using BlazorGPT.Components.Memories;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Memories;

public class MemoriesPlugin(IServiceProvider serviceProvider)
{
    [KernelFunction]
    [Description("Returns citations from users memories based on a query")]
    [return: Description("A list of citations")]
    public async Task<ReturnCitationsList> GetMemories(
        [Description("The topic, story, event etc that users needs to know more about")]
        string query,
        [Description("The relevance of the search query, a value between 0 and 1")]
        double relevance = 0.50,
        [Description("The index to search in")]
        string index = MemoriesService.IndexDefault,
        [Description("The number of results to return")]
        int limit = 3
    )
    {
        var docService = serviceProvider.GetRequiredService<MemoriesService>();
        var documents = await docService.SearchUserDocuments(query, index, relevance!, limit);

        var list = new ReturnCitationsList();
        list.AddRange(documents);

        return list;
    }
}