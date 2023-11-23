using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Planning;

namespace BlazorGPT.Pipeline.Interceptors;

public class BlazorFormGenerator : InterceptorBase, IInterceptor
{
    private readonly PipelineOptions _options;
    private readonly KernelService _kernelService;


    public BlazorFormGenerator(IDbContextFactory<BlazorGptDBContext> context,
        ConversationsRepository conversationsRepository,
        KernelService kernelService
        , IOptions<PipelineOptions> options) : base(context, conversationsRepository)
    {
        _options = options.Value;
        _kernelService = kernelService;
    }

    public override string Name { get; } = "BlazorFormGenerator";

    public Task<Conversation> Receive(IKernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(conversation);
    }

    public async Task<Conversation> Send(IKernel kernel, Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Messages.Count() == 2) await Play(kernel, conversation);
        return conversation;
    }

    private async Task Play(IKernel kernel, Conversation conversation)
    {
        var skillsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
        kernel.ImportSemanticFunctionsFromDirectory(skillsDirectory, "BlazorFormsGenerator");

        var ask = conversation.Messages.Last().Content;

        var p = new Plan("Wait for instructions");
        conversation.SKPlan = p.ToJson(true);
        OnUpdate?.Invoke();

        var ctx = kernel.CreateNewContext();
        ctx.Variables["input"] = ask;

        var actionPlanner = new ActionPlanner(kernel, new ActionPlannerConfig());
        var actionPlan = await actionPlanner.CreatePlanAsync(ask);

        var memory = await _kernelService.GetMemoryStore();
        var planner = new SequentialPlanner(kernel,
            new SequentialPlannerConfig
            { SemanticMemoryConfig = new SemanticMemoryConfig { RelevancyThreshold = 0.5, Memory = memory } });
        var plan = await planner.CreatePlanAsync(ask);
        conversation.SKPlan = plan.ToJson(true);
        OnUpdate?.Invoke();

        var newPlan = plan;
        while (newPlan.HasNextStep)
        {
            newPlan = await newPlan.InvokeNextStepAsync(ctx);
            conversation.SKPlan = newPlan.ToJson(true);
            OnUpdate?.Invoke();
        }

        ExtractEmbeddingsTag(conversation, newPlan);

        ExtractResult(conversation, newPlan);

        conversation.StopRequested = true;
    }

    private static void ExtractResult(Conversation conversation, Plan newPlan)
    {
        var planResultKey = "PLAN.RESULT";
        newPlan.State.TryGetValue(planResultKey, out var planResult);
        if (planResult != null)
        {
            conversation.Messages.Add(new ConversationMessage("assistant", planResult));
        }
        else
        {
            newPlan.State.TryGetValue("RESULT__FINAL_ANSWER", out var result);
            if (result != null) conversation.Messages.Add(new ConversationMessage("assistant", result));
        }
    }

    private static void ExtractEmbeddingsTag(Conversation conversation, Plan newPlan)
    {
        var embeddingOutputName =
            newPlan.Steps.FirstOrDefault(s => s.Name == "IncludeEmbeddingsTag")?.Outputs.FirstOrDefault();
        if (embeddingOutputName != null)
        {
            newPlan.State.TryGetValue(embeddingOutputName, out var embeddings);
            if (embeddings != null)
                conversation.Messages.Last().Content = embeddings + "\n" + conversation.Messages.Last().Content;
        }
    }
}