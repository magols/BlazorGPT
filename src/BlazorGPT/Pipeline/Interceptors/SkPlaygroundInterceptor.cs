using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using BlazorGPT.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning.Sequential;
using Microsoft.SemanticKernel.Skills.Web.Bing;
using Microsoft.SemanticKernel.Skills.Web;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SkPlaygroundInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        private readonly PipelineOptions _options;
        public string Name { get; } = "SkPlayground";
        public bool Internal { get; } = false;

        public SkPlaygroundInterceptor(IDbContextFactory<BlazorGptDBContext> context, 
            ConversationsRepository conversationsRepository,
            KernelService kernelService
            ,IOptions<PipelineOptions> options) : base(context, conversationsRepository)
        {
            _options = options.Value;
            _kernelService = kernelService;
        }

        public Task<Conversation> Receive(IKernel kernel, Conversation conversation)
        {

            return Task.FromResult(conversation);
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation)
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
            kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "SkPlayground");
            kernel.ImportSkill(new EmbeddingSkill(kernel), "embeddings");
            kernel.ImportSkill(new TranslateSkill(kernel), "summarize");

            var bingConnector = new BingConnector(_options.BING_API_KEY);
            var bing = new WebSearchEngineSkill(bingConnector);
            var search = kernel.ImportSkill(bing, "bing");

            var ask =  conversation.Messages.Last().Content;

            SKContext ctx = kernel.CreateNewContext();
            ctx["input"] = ask;

            var planner = new SequentialPlanner(kernel, new SequentialPlannerConfig {  RelevancyThreshold = 0.8 });

            var plan = await planner.CreatePlanAsync(ask);
         
            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();

            return;

            Plan newPlan = plan;
            while (newPlan.HasNextStep)
            {
               newPlan = await newPlan.InvokeNextStepAsync(ctx);
               conversation.SKPlan = newPlan.ToJson(true);
               OnUpdate?.Invoke();
            }

            ExtractEmbeddingsTag(conversation, newPlan);

            newPlan.State.TryGetValue("RESULT__FINAL_ANSWER", out string? result);
            if (result != null)
            {
                conversation.Messages.Add(new ConversationMessage("assistant", result));
                conversation.StopRequested = true;
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
