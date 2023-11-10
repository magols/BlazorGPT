using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using BlazorGPT.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;
 
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Plugins.Core;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SkPlaygroundInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        private readonly PipelineOptions _options;
        public override string Name { get; } = "SkPlayground";
        public override bool Internal { get; } = false;

        public SkPlaygroundInterceptor(IDbContextFactory<BlazorGptDBContext> context, 
            ConversationsRepository conversationsRepository,
            KernelService kernelService
            ,IOptions<PipelineOptions> options) : base(context, conversationsRepository)
        {
            _options = options.Value;
            _kernelService = kernelService;
        }

        public Task<Conversation> Receive(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {

            return Task.FromResult(conversation);
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {
            if (conversation.Messages.Count() == 2)
            {
                await Play(kernel, conversation);
            }
            return conversation;
        }

        private async Task Play(IKernel kernel, Conversation conversation)
        {
            var skillsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            kernel.ImportSemanticFunctionsFromDirectory(skillsDirectory, "SkPlayground");
            kernel.ImportFunctions(new EmbeddingSkill(kernel, await _kernelService.GetMemoryStore()), "embeddings");
            kernel.ImportFunctions(new TranslateSkill(kernel), "summarize");
            
            var bingConnector = new BingConnector(_options.BING_API_KEY);
            var bing = new WebSearchEngineSkill(bingConnector);
            // todo: broken plugin
            //var search = kernel.ImportFunctions(bing, "bing");

            kernel.ImportFunctions(new TimePlugin(), "time");

            var ask =  conversation.Messages.Last().Content;


            var p = new Plan("Wait for instructions");
            conversation.SKPlan = p.ToJson(true);
            OnUpdate?.Invoke();


 

            SKContext ctx = kernel.CreateNewContext();
            ctx.Variables["input"] = ask;

            var planner = new SequentialPlanner(kernel, new SequentialPlannerConfig { });

            var plan = await planner.CreatePlanAsync(ask);
         
            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();


            Plan newPlan = plan;
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
            string planResultKey = "PLAN.RESULT";
            newPlan.State.TryGetValue(planResultKey, out string? planResult);
            if (planResult != null)
            {
                conversation.Messages.Add(new ConversationMessage("assistant", planResult));
            }
            else
            {
                newPlan.State.TryGetValue("RESULT__FINAL_ANSWER", out string? result);
                if (result != null)
                {
                    conversation.Messages.Add(new ConversationMessage("assistant", result));
                }

            }



        }

        private static void ExtractEmbeddingsTag(Conversation conversation, Plan newPlan)
        {
            var embeddingOutputName =
                newPlan.Steps.FirstOrDefault(s => s.Name == "IncludeEmbeddingsTag")?.Outputs.FirstOrDefault();
            if (embeddingOutputName != null)
            {
                newPlan.State.TryGetValue(embeddingOutputName, out string? embeddings);
                if (embeddings != null)
                {
                    conversation.Messages.Last().Content = embeddings + "\n" + conversation.Messages.Last().Content;
                }
            }
        }
    }
}
