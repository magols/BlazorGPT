using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using System.Numerics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;
using BlazorGPT.Migrations;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SKSequentialPlannerInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        private CancellationToken _cancellationToken;
        public string Name { get; } = "SKSequentialPlanner";
        public bool Internal { get; } = false;

        public SKSequentialPlannerInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository, KernelService kernelService) : base(context, conversationsRepository)
        {
            _kernelService = kernelService;
        }

        public Task<Conversation> Receive(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            return Task.FromResult(conversation);
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            //if (conversation.Messages.Count() == 2)
            //{
                await AppendInstruction(kernel, conversation);
            //}
            return conversation;
        }

        private async Task AppendInstruction(IKernel kernel, Conversation conversation)
        {

            //   var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            //var pluginsDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Plugins");
            var pluginsDirectory = Path.Combine("C:\\Source\\BlazorGPT\\src\\BlazorGPT.Web", "Plugins");

            kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "OrchestratorPlugin");
            kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "SummarizeSkill");
            kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, "Samples");


            // Add the math plugin
            var mathPlugin = kernel.ImportSkill(new MathPlugin(), "MathPlugin");

            // Create a planner
            var planner = new SequentialPlanner(kernel);

            //var ask = "I have $2130.23. How much would I have after it grew by 24% and after I spent $5 on a latte?";

            var ask = "What is 2+2? Explain the math in danish";
            var plan = await planner.CreatePlanAsync(ask);


            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();

            // Console.WriteLine("Plan:\n");
            // Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));

            var result = await plan.InvokeAsync();


            // insert a new message with the result of the plan
            conversation.Messages.Insert(conversation.Messages.Count -1, new ConversationMessage("assistant", "Plan :\n\n"  + plan.ToJson(true) + "\n\n"));
            conversation.Messages.Insert(conversation.Messages.Count - 1, new ConversationMessage("assistant", "Plan result:\n\n" + result.Result + "\n\n"));
            
            conversation.Messages.Last().Content = result.Result.Trim();


        }


    }
}
