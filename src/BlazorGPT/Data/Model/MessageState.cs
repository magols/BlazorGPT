namespace BlazorGPT.Data.Model;

public class MessageState : StateDataBase {
    public Guid? MessageId { get; set; }
    public ConversationMessage? Message { get; set; }
}