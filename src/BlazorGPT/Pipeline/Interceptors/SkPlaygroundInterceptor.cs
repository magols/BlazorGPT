using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using System.Numerics;
using Microsoft.SemanticKernel.Skills.Core;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SkPlaygroundInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        public string Name { get; } = "SkPlayground";
        public bool Internal { get; } = false;

        public SkPlaygroundInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository, KernelService kernelService) : base(context, conversationsRepository)
        {
            _kernelService = kernelService;
        }

        public Task<Conversation> Receive(IKernel kernel, Conversation conversation)
        {

            return Task.FromResult(conversation);
        }

        public async Task<Conversation> Send(IKernel kernel, Conversation conversation)
        {
            //if (conversation.Messages.Count() == 2)
            //{
                await AppendInstruction(kernel, conversation);
            //}
            return conversation;
        }

        private async Task AppendInstruction(IKernel kernel, Conversation conversation)
        {

            var skillsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");

            kernel.ImportSkill(new TextMemorySkill(), "memory");

            var ask =  conversation.Messages.Last().Content;
            //kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "SkPlayground", "SemanticMemorySkill");
            kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "SkPlayground");

            var planner = new SequentialPlanner(kernel);
            var plan = await planner.CreatePlanAsync(ask);

            
            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();
            var result = await plan.InvokeAsync();

            conversation.Messages.Last().Content += $"{result.Result}";


        }


    }
}
