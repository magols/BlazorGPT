using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using System.Numerics;
using Microsoft.SemanticKernel.Skills.Core;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SemanticKernelPlanInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        public string Name { get; } = "SK Plan";
        public bool Internal { get; } = false;

        public SemanticKernelPlanInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository, KernelService kernelService) : base(context, conversationsRepository)
        {
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
                await AppendInstruction(kernel, conversation);
            }
            return conversation;
        }

        private async Task AppendInstruction(IKernel kernel, Conversation conversation)
        {

            var skillsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "skills");

            kernel.ImportSkill(new TextMemorySkill(kernel.Memory));
            //kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "SummarizeSkill");
            //kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "WriterSkill");
            kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "Translate");

            var ask =  conversation.Messages.Last().Content;
            var planner = new SequentialPlanner(kernel);
            var originalPlan = await planner.CreatePlanAsync(ask);
            conversation.SKPlan = originalPlan.ToJson(true);


            var result = await originalPlan.InvokeAsync();

            //conversation.Messages.Last().Content += $"{result.Result}";
            //conversation.AddMessage("assistant", result.Result);
        }


    }
}
