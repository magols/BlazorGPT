using System.ComponentModel;
using Microsoft.SemanticKernel.SkillDefinition;

namespace BlazorGPT.Web;

public class RandomReasonSkill
{

    private string[] reasons = new string[] {
        "I'm not ready",
        "I'm not ready yet",
        "I'm not ready for that" };

    [SKFunction, Description("Returns a random reason")]
    public string GetReason()
    {
        var random = new Random();
        return reasons[random.Next(reasons.Length)];
    }
}