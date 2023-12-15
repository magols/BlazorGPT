using BlazorGPT.Data.Model;
using BlazorGPT.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlazorGPT.Data
{

    public class ConversationCreatorAndDeleter
    {
        private readonly IDbContextFactory<BlazorGptDBContext> _dbContextFactory;
        private readonly QuickProfileRepository _quickProfileRepository;
        private readonly ScriptRepository _scriptRepository;
        private readonly ConversationsRepository _conversationsRepository;
        private readonly PipelineOptions _options;

        public ConversationCreatorAndDeleter(IDbContextFactory<BlazorGptDBContext> dbContextFactory, 
            ConversationsRepository conversationsRepository,
            ScriptRepository scriptRepository,
            QuickProfileRepository quickProfileRepository,
            IOptions<PipelineOptions> options)
        {
            _options = options.Value;
            _conversationsRepository = conversationsRepository;
            _dbContextFactory = dbContextFactory;
            _quickProfileRepository = quickProfileRepository;
            _scriptRepository = scriptRepository;
        }


        public async Task<Conversation> CreateConversation(string summary)
        {
            string userId = "df2191cc-160c-49f0-a136-8cbdbba09200";


            // create a conversation
            var conversation = new Conversation()
            {
                UserId = userId,
                Model = _options.Model!,
                DateStarted = DateTime.Now,
                Summary = summary
            };

            // add some chatmessages to the convrsation
            var messages = new List<ConversationMessage>();
            
            conversation.AddMessage(new ConversationMessage("system","You are helping me debug this ChatpGPT app I am building"));
            conversation.AddMessage(new ConversationMessage("user","I am using the GPT-3 API to generate responses"));
            conversation.AddMessage(new ConversationMessage("assistant", "I will try to help you"));


            // add the conversation to the context
            await using var ctx = await _dbContextFactory.CreateDbContextAsync();
            ctx.Conversations.Add(conversation);
            await ctx.SaveChangesAsync();

            return conversation;

        }



    }


    public class SampleDataSeeder
    {
        private ScriptRepository _scriptRepository;
        private readonly IDbContextFactory<BlazorGptDBContext> _dbContextFactory;
        private QuickProfileRepository _quickProfileRepository;

        public SampleDataSeeder(IDbContextFactory<BlazorGptDBContext> dbContextFactory, ScriptRepository scriptRepository, QuickProfileRepository quickProfileRepository)
        {
            _dbContextFactory = dbContextFactory;
            _quickProfileRepository = quickProfileRepository;
            _scriptRepository = scriptRepository;
        }

        public async Task SeedQPDataForUser(string userId)
        {
            var ctx = await _dbContextFactory.CreateDbContextAsync();

            var profiles = new List<QuickProfile>();

            profiles.Add(new QuickProfile()
            {   
                UserId = userId,
                Name = "MD",
                EnabledDefault = true,
                InsertAt = InsertAt.Before,
                Content = "Format reply in Markdown if it contains code or or other formatting"
                
            });

            profiles.Add(new QuickProfile()
            {
                UserId = userId,


                Name = "C#",
                EnabledDefault = false,
                InsertAt = InsertAt.Before,
                Content = "I use C#"

            });

            profiles.Add(new QuickProfile()
            {UserId = userId,


                Name = "Dad joke",
                EnabledDefault = false,
                InsertAt = InsertAt.Before,
                Content = "Include a dad joke about this at the end."

            });

            profiles.Add(new QuickProfile()
            {
                UserId = userId,
                Name = "Shakespeare",
                EnabledDefault = false,
                InsertAt = InsertAt.Before,
                Content = "Reply in the style of Shakespeare"

            });

            profiles.Add(new QuickProfile()
            {
                UserId = userId,
                Name = "C4 Structurizr",
                EnabledDefault = false,
                InsertAt = InsertAt.Before,
                Content = @"
Going forward, I want you to act as a software architect and build C4 diagrams using Structurizr DSL. Keep the answers to markdown syntax and always keep in mind software functional and non-functional aspects. 

Main goal is to make a basic model of a system, a few applications in that system, and their dependencies reflected into context, containers, components
"
            });


            profiles.Add(new QuickProfile()
            {
                UserId = userId,
                Name = "Tweet",
                EnabledDefault = false,
                InsertAt = InsertAt.After,
                Content = "Write a tweet about this. It should include a hash tag and one emoji"

            });

            profiles.Add(new QuickProfile()
            {UserId = userId,
                Name = "Linkedin",
                EnabledDefault = false,
                InsertAt = InsertAt.After,
                Content = "Formulate the generated text as a Linkedin post. Keep in mind that the maximum length is 3000 characters. Structure the main points of the text into a bulleted list. Start with an exciting teaser sentence and end with a call to action for more engagement."
            });

            profiles.Add(new QuickProfile()
            {
                UserId = userId,
                Name = "Summarize 3",
                EnabledDefault = false,
                InsertAt = InsertAt.After,
                Content = "Summarize this into 3 bullet points"

            });


            ctx.QuickProfiles.AddRange(profiles);

            await ctx.SaveChangesAsync();
        }

        public async Task SeedScriptsDataForUser(string userId)
        {
            await using var ctx = await _dbContextFactory.CreateDbContextAsync();

            var scripts = new List<Script>();

            scripts.Add(new Script()
            {
                UserId = userId,
                Name = "Comedian bio",
                SystemMessage = "You are a helpful research assistant for blog posts",
                Steps = new List<ScriptStep>()
                {
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 1,
                        Message = "Introduction: Start with a brief introduction of the comedian {0}, their style of comedy, and why they are popular or significant in the comedy world."
                    },
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 2,
                        Message =
                            "Early life and career: Provide some background information about the comedian's early life and career, such as where they were born, their education, and how they got started in comedy."
                    },

                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 3,
                        Message = "Famous works or performances: Highlight some of the comedian's most famous works or performances, such as a popular TV show, movie, or stand-up special. Include some of their best jokes or comedic moments to give readers a taste of their style."
                    },
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 4,
                        Message = "Impact and legacy: Discuss the comedian's impact on the comedy world and their legacy. This could include how they influenced other comedians or how they broke new ground in comedy."
                    },

                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 5,
                        Message = "Conclusion: Summarize the key points and provide a final thought on the comedian's overall significance and appeal."
                    },
                }

            });


            scripts.Add(new Script()
            {
                UserId = userId,
                Name = "Article and social media posts about X",
                SystemMessage = "You are a helpful assistant helping with social media",
                Steps = new List<ScriptStep>()
                {
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 1,
                        Message = "Write a 100 word article about {0}. Include a short intro. In the end, summarize into a few bullet points"
                    },
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 2,
                        Message = "Create a tweet about this"
                    },
                    new ScriptStep()
                    {
                        Role = "user",
                        SortOrder = 3,
                        Message = "Formulate the generated text as a Linkedin post. Keep in mind that the maximum length is 3000 characters. Structure the main points of the text into a bulleted list. Start with an exciting teaser sentence and end with a call to action for more engagement."
                    },
                }

            });

            ctx.Scripts.AddRange(scripts);
            await ctx.SaveChangesAsync();

        }
    }
}
