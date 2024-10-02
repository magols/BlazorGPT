using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.SemanticKernel;

namespace BlazorGPT.Pipeline;

public interface IFunctionCallingUserProvider
{
    Task<string> GetUserId();
}

public class FunctionCallingUserConsoleProvider : IFunctionCallingUserProvider
{ 
    public Task<string> GetUserId()
    {
        return Task.FromResult("BotUser");
    }
}

public class FunctionCallingUserWebProvider(UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor) : IFunctionCallingUserProvider
{
    public async Task<string> GetUserId()
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

        return user.Id;
    }
}

public class FunctionCallingFilter(CurrentConversationState conversationState, IFunctionCallingUserProvider userProvider) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var userId = await userProvider.GetUserId();
        var conversation = conversationState.GetCurrentConversation(userId);

        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation is null. This filter requires a conversation to be set.");
        }

        var sw = new Stopwatch();
        sw.Start();
        await next(context);
        sw.Stop();

        var elapsedTime = sw.Elapsed.Seconds > 0 ? sw.Elapsed.Seconds + "s": sw.ElapsedMilliseconds + "ms"; 

        StringBuilder sb = new StringBuilder();
        sb.Append($"\n\n##### {context.Function.PluginName} {context.Function.Name}\n\n");
        sb.Append($"<span style=\"font-size: smaller;color: green;\">{elapsedTime}" + "</span>  \n");
        foreach (var arg in context.Arguments.Names)
        {
            sb.Append("* " + arg + " : " + context.Arguments[arg] + "\n");
        }
        sb.Append("  \n");
        sb.Append("Result:\n" + context.Result + "\n");

       var lastUserMessage =  conversation.Messages.FindLast(o => o.Role == ConversationRole.User)!;

       if (!string.IsNullOrEmpty(lastUserMessage?.ActionLog))
           lastUserMessage.ActionLog += "\n\n---" + sb;
       else
           lastUserMessage.ActionLog = sb.ToString();
    }
}


