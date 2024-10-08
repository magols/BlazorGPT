using System.ComponentModel;
using BlazorGPT.Components.Memories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace Memories;

public class MemoriesPlugin(IServiceProvider serviceProvider)
{
    [KernelFunction]
    [Description("Returns citations from users memories based on a query")]
    [return: Description("A list of citations")]
    public async Task<IEnumerable<Citation>> GetMemories(
        [Description("The topic, story, event etc that users needs to know more about")]
        string query, 
        string? index = MemoriesService.IndexDefault)
    {
        var docService = serviceProvider.GetRequiredService<MemoriesService>();
        return await docService.SearchUserDocuments(query, index,0.65, 3);
    }
}