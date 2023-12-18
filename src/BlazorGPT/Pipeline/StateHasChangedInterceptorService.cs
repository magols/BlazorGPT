namespace BlazorGPT.Pipeline;

public class StateHasChangedInterceptorService
{
    public event Func<Task>? OnConversationUpdated;

    public async Task Broadcast(Guid conversationId)
    {
        if (OnConversationUpdated != null)
        {
            await OnConversationUpdated.Invoke();
        }
    }

    public async Task ConversationUpdated(Guid conversationId)
    {
        await Broadcast(conversationId);
    }
}