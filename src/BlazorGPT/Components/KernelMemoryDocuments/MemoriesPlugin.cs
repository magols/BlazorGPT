using System.ComponentModel;
using BlazorGPT.Components.KernelMemoryDocuments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;

namespace Memories;

public class MemoriesPlugin(IServiceProvider serviceProvider)
{
    [KernelFunction]
    [Description("Returns citations from users memories based on a query")]
    [return: Description("A list of citations")]
    public async Task<IEnumerable<Citation>> GetUserDocumentsFromMemory(
        [Description("The topic, story, event etc that users needs to know more about")]
        string query)
    {

        var docService = serviceProvider.GetRequiredService<MemoriesService>();
        return await docService.SearchUserDocuments(query, 0.7, 3);
    }
}