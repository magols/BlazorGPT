namespace BlazorGPT.Data.Model;

public class QuickProfile
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public string Content { get; set; } = default!;

    public InsertAt InsertAt { get; set; }

    public string? UserId { get; set; } = default!;

    public bool EnabledDefault { get; set; }
    public List<Conversation>? Conversations { get; set; }
}