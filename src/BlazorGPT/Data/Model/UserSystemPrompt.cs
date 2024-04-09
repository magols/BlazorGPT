using System.ComponentModel.DataAnnotations;

namespace BlazorGPT.Data.Model;

public class UserSystemPrompt
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public string Text { get; set; }
    
    [Required]
    public string? UserId { get; set; }
 
}