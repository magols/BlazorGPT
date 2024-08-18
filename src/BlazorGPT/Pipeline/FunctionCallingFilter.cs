using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public class FunctionCallingFilter(CurrentConversationState conversationState, UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (httpContextAccessor.HttpContext == null)
        {
            throw new InvalidOperationException("HttpContext is null. This filter requires HttpContext to be set.");
        }

        var user = await userManager.GetUserAsync(httpContextAccessor.HttpContext.User);

        if (user == null)
        {
            throw new InvalidOperationException("User is null. This filter requires a user to be set.");
        }

        var conversation = conversationState.GetCurrentConversation(user.Id);

        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation is null. This filter requires a conversation to be set.");
        }


        await next(context);

        StringBuilder sb = new StringBuilder();

        sb.Append($"**{context.Function.PluginName} {context.Function.Name}** called with arguments:  \n");

        foreach (var arg in context.Arguments.Names)
        {

            sb.Append("* " + arg + " : " + context.Arguments[arg] + "\n");

        }
        sb.Append("  \n");

        sb.Append("Result: " + context.Result);

        sb.Append("  \n");

        if (string.IsNullOrEmpty(conversation.SKPlan))
        {
            conversation.SKPlan = sb.ToString();

        }
        else
        {
            conversation.SKPlan += "  \n" + sb.ToString();
        }
    }
}