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
        string? index = MemoriesService.IndexDefault)
    {
        var docService = serviceProvider.GetRequiredService<MemoriesService>();
        var documents = await docService.SearchUserDocuments(query, index,0.65, 3);

        var list = new ReturnCitationsList();
        list.AddRange(documents);
       
        return list;
    }
}