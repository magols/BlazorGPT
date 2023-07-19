using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Skills.Core;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Skills.Web;
using Microsoft.SemanticKernel.Skills.Web.Bing;

namespace BlazorGPT.Pipeline.Interceptors
{
    public class SkPlaygroundInterceptor : InterceptorBase, IInterceptor
    {
        private KernelService _kernelService;
        private PipelineOptions _options;
        public string Name { get; } = "SkPlayground";
        public bool Internal { get; } = false;

        public SkPlaygroundInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository, KernelService kernelService, IOptions<PipelineOptions> options) : base(context, conversationsRepository)
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
            //if (conversation.Messages.Count() == 2)
            //{
                await AppendInstruction(kernel, conversation);
            //}
            return conversation;
        }

        private async Task AppendInstruction(IKernel kernel, Conversation conversation)
        {

            //var skillsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Plugins");
            ////kernel.ImportSemanticSkillFromDirectory(skillsDirectory, "Samples");

            using var bingConnector = new BingConnector(_options.BING_API_KEY);
            var webSearchEngineSkill = new WebSearchEngineSkill(bingConnector);

            kernel.ImportSkill(webSearchEngineSkill, "WebSearch");
            kernel.ImportSkill(new TimeSkill(), "time");

            var ask =  conversation.Messages.Last().Content;


            var config = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig();
            config.MinIterationTimeMs = 1500;
            config.MaxTokens = 2800;


            var planner = new StepwisePlanner(kernel, config);
            var plan =   planner.CreatePlan(ask);

            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();

            SKContext ctx = kernel.CreateNewContext();

            plan = await plan.InvokeNextStepAsync(ctx);

            var result = await plan.InvokeAsync(ctx);


            Console.WriteLine("Result: " + result);
            if (result.Variables.TryGetValue("stepCount", out string? stepCount))
            {
                Console.WriteLine("Steps Taken: " + stepCount);
            }

            if (result.Variables.TryGetValue("skillCount", out string? skillCount))
            {
                Console.WriteLine("Skills Used: " + skillCount);
            }

            // insert a new message with the result of the plan
            conversation.Messages.Insert(conversation.Messages.Count-1, new ConversationMessage("assistant", "Plan result:\n\n" + plan.State.Input + "\n\n"));

            conversation.Messages.Last().Content = $"hello";

            conversation.SKPlan = plan.ToJson(true);
            OnUpdate?.Invoke();


            return;

            int step = 1;
            int maxSteps = 10;
            try
            {
                while (plan.HasNextStep)
                {
                    if (string.IsNullOrEmpty(ask))
                    {
                        await plan.InvokeNextStepAsync(ctx);
                        // or await kernel.StepAsync(plan);
                    }
                    else
                    {
                        // plan = await plan.InvokeNextStepAsync(ctx);
                        await kernel.StepAsync(ask, plan);
                        ask = string.Empty;


                    }

                    conversation.SKPlan = plan.ToJson(true);
                    OnUpdate?.Invoke();

                    if (!plan.HasNextStep)
                    {
                        Console.WriteLine($"Step {step} - COMPLETE!");
                        Console.WriteLine(plan.State.ToString());
                        break;
                    }

                    Console.WriteLine($"Step {step} - Results so far:");
                    Console.WriteLine(plan.State.ToString());
                }


                //for (int step = 1; plan.HasNextStep && step < maxSteps; step++)
                //{
                //    if (string.IsNullOrEmpty(ask))
                //    {
                //        await plan.InvokeNextStepAsync(kernel.CreateNewContext());
                //        // or await kernel.StepAsync(plan);
                //    }
                //    else
                //    {
                //        plan = await kernel.StepAsync(ask, plan);
                //        ask = string.Empty;


                //    }

                //    conversation.SKPlan = plan.ToJson(true);
                //    OnUpdate?.Invoke();

                //    if (!plan.HasNextStep)
                //    {
                //        Console.WriteLine($"Step {step} - COMPLETE!");
                //        Console.WriteLine(plan.State.ToString());
                //        break;
                //    }

                //    Console.WriteLine($"Step {step} - Results so far:");
                //    Console.WriteLine(plan.State.ToString());
                //}
            }
            catch (KernelException e)
            {
                Console.WriteLine("Step - Execution failed:");
                Console.WriteLine(e.Message);
            }




        }



        public  async Task RunWithQuestion(IKernel kernel, string question)
        {
            using var bingConnector = new BingConnector(_options.BING_API_KEY);
            var webSearchEngineSkill = new WebSearchEngineSkill(bingConnector);

            kernel.ImportSkill(webSearchEngineSkill, "WebSearch");
            //kernel.ImportSkill(new LanguageCalculatorSkill(kernel), "advancedCalculator");
            // kernel.ImportSkill(new SimpleCalculatorSkill(kernel), "basicCalculator");
            kernel.ImportSkill(new TimeSkill(), "time");

            Console.WriteLine("*****************************************************");
            Stopwatch sw = new();
            Console.WriteLine("Question: " + question);

            var config = new Microsoft.SemanticKernel.Planning.Stepwise.StepwisePlannerConfig();
            config.ExcludedFunctions.Add("TranslateMathProblem");
            config.MinIterationTimeMs = 1500;
            config.MaxTokens = 2500;

            StepwisePlanner planner = new(kernel, config);
            sw.Start();
            var plan = planner.CreatePlan(question);

            var result = await plan.InvokeAsync(kernel.CreateNewContext());
            Console.WriteLine("Result: " + result);
            if (result.Variables.TryGetValue("stepCount", out string? stepCount))
            {
                Console.WriteLine("Steps Taken: " + stepCount);
            }

            if (result.Variables.TryGetValue("skillCount", out string? skillCount))
            {
                Console.WriteLine("Skills Used: " + skillCount);
            }

            Console.WriteLine("Time Taken: " + sw.Elapsed);
            Console.WriteLine("*****************************************************");
        }

    }
}
