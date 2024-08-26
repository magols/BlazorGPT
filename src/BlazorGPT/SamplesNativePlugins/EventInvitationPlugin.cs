using System.ComponentModel;
using BlazorGPT.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace BlazorGPT.SamplesNativePlugins;

/*  
 * EventInvitationPlugin
 * 
 * This plugin is used to manage invitations to a specific event.
 * the plugin should suggest two different dates that the user can choose from.
 * the plugin should also handle to subscribe and unsubscribe to either of the dates.
 * the user should be prompted to state his/her name and which date they would like to attend. Unsubscribing should be done by stating the name.
 * the plugin should use the semantic kernel memory to keep track of which user is subscribed to which date.
 *
 *
 * The plugin should have the following functions:
 * string Subscribe(string name, string date)  
 * string UnsubscribeFromEvent(string name)
 * IEnumerable<string> ListSubscribers()
 *



 */
public class EventInvitationPlugin
{
    private IServiceProvider _serviceProvider;
    private readonly string _memoryModel = "text-embedding-ada-002";
    private const string Collection = "gpt_event";

    private async Task<ISemanticTextMemory> GetStore()
    {
        var kernelService = _serviceProvider.GetRequiredService<KernelService>();
        var store = await kernelService.GetMemoryStore(EmbeddingsModelProvider.OpenAI, _memoryModel);
        return store;
    }
    public EventInvitationPlugin(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }



    [KernelFunction("event_subscribe")]
    [Description("Add a user to the event")]
    public async Task<string> Subscribe(string name, string date)
    {
        var store = await GetStore();
        string metadata = $"Subscribed {name} to {date}";
        await store.SaveInformationAsync(Collection, metadata, Guid.NewGuid().ToString());
        return $"Subscribed {name} to {date}";
    }

    [KernelFunction("event_unsubscribe")]
    [Description("Remove a user from the event")]
    public async Task<string> UnsubscribeFromEvent(string name)
    {
        var store = await GetStore();
        var result = store.SearchAsync(Collection, name, 1, 0.4);
        string? exists = null;
        await foreach (var item in result)
        {
            exists = item.Metadata.Text;
            await store.RemoveAsync(Collection, item.Metadata.Id);
        }
        return $"Unsubscribed {name} from the event";
    }

    [KernelFunction("event_list_subscribers")]
    [Description("List all subscribers to the event")]
    public async Task<List<string>> ListSubscribers()
    {
        var query = """
                    List all subscribers to the event
                    """;
        var store = await GetStore();
        var todos = store.SearchAsync(Collection, query, 500, 0.4);
        List<string> todoList = new List<string>();
        await foreach (var todo in todos)
        {
            var text = todo.Metadata.Text;
            todoList.Add(text);
        }
        return todoList;
    }

    // clear all data 
    [KernelFunction("event_clear")] 
    [Description("Clear all data")]
    [return: Description("Number of items cleared")]
    public async Task<int> Clear()
    {
        var store = await GetStore();

        var result = store.SearchAsync(Collection, "subscribed", 500, 0.0);

        int count = 0;

        await foreach (var item in result)
        {
            await store.RemoveAsync(Collection, item.Metadata.Id);
            count++;
        }
        return count;
    }
}