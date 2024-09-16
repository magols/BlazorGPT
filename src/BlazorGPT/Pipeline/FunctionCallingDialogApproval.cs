using Microsoft.SemanticKernel;
using Radzen;

namespace BlazorGPT.Pipeline;

public interface IFunctionApprovalService
{
    Task<bool?> IsInvocationApproved(KernelFunction function, KernelArguments arguments);
}

public class FunctionCallingDialogApprovalService(DialogService dialogService) : IFunctionApprovalService
{
    public async Task<bool?> IsInvocationApproved(KernelFunction function, KernelArguments arguments)
    {
        var confirm = await  dialogService.Confirm($"{function.PluginName} - {function.Name}", "Perform function call?");
        return confirm;
    }
}

public class FunctionApprovalFilter(IFunctionApprovalService approvalService, NotificationService notificationService) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Description.ToLower().Contains("requires approval"))
        {
            var approval = await approvalService.IsInvocationApproved(context.Function, context.Arguments);
            if (approval.HasValue && approval.Value)
            {
                await next(context);
            }
            else
            {
                context.Result = new FunctionResult(context.Result, "Operation was rejected by user.");
                notificationService.Notify(NotificationSeverity.Warning, "Operation was rejected by user.");
            }
        }
        else
        {
            await next(context);
        }
    }
}