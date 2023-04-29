using BlazorGPT.Data.Model;

namespace BlazorGPT.Data;

public class QuickProfile
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Content { get; set; }

    public InsertAt InsertAt { get; set; }

    public string? UserId { get; set; }

    public bool EnabledDefault { get; set; }
    public List<Conversation>? Conversations { get; set; }
}