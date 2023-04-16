namespace BlazorGPT.Managers;

public class StateManager
{
    // eventcallback for state change


    public event Func<Task>? OnConversationUpdated;

    public async Task Broadcast(Guid conversationId)
    {
        if (OnConversationUpdated != null)
        {
            Console.WriteLine("Invoking Broadcast");
            await OnConversationUpdated.Invoke();
        }
    }

    public async Task ConversationUpdated(Guid conversationId)
    {
        Console.WriteLine("Updates for " + conversationId);
        await Broadcast(conversationId);
    }
}