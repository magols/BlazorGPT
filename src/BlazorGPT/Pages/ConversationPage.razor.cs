using System.ComponentModel.DataAnnotations;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Shared;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Radzen;
using Radzen.Blazor;

namespace BlazorGPT.Pages
{
    public partial class ConversationPage : IDisposable
    {
        public class PromptModel
        {
            [Required]
            public string? Prompt { get; set; }
        }

        [CascadingParameter(Name = "UserId")]
        public string UserId { get; set; } = null!;

        [Parameter]
        public Guid? ConversationId { get; set; }

        [Parameter]
        public Guid? MessageId { get; set; }
        PromptModel Model = new();

        [Inject]
        public IOptions<PipelineOptions> PipelineOptions { get; set; } = null!;

        [Inject]
        public QuickProfileRepository QuickProfileRepository { get; set; }

        [Inject]
        public IResizeListener ResizeListener { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;
        [Inject]
        public IOpenAIService Ai { get; set; }


        [Inject]
        public IDbContextFactory<BlazorGptDBContext> DbContextFactory { get; set; } = null!;
        [Inject]
        public ScriptRepository ScriptRepository { get; set; } = null!;

        [Inject]
        public ConversationsRepository ConversationsRepository { get; set; }
        [Inject]
        public DialogService DialogService { get; set; } = null!;

        [Inject]
        public IInterceptorHandler  InterceptorHandler{ get; set; }

        [Inject]
        public ConversationInterop? Interop { get; set; }
        public Conversation Conversation = new();

        private GPTModelSelector? _modelSelector;

        bool promptIsReady;
        string scriptInput;
        bool showTokens = false;

        private int controlHeight => _browserIsSmall ? 400 : 350;
        protected override async Task OnParametersSetAsync()
        {
            await SetupConversation();
        }

   

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                ResizeListener.OnResized += WindowResized;
                _browserIsSmall = await ResizeListener.MatchMedia(Breakpoints.SmallDown);
                await Interop.SetupCopyButtons();

            }

            if (selectedTabIndex == 0)
            {
                await Interop.FocusElement(_promptField2.Element);
            }

            await Interop.ScrollToBottom("message-pane");
        }
        
        async Task SetupConversation()
        {
            if (ConversationId != null)
            {
                var ctx = DbContextFactory.CreateDbContext();
                var loaded  = await ConversationsRepository.GetConversation(ConversationId);
                if (loaded != null)
                {
                    if (loaded.BranchedFromMessageId != null)
                    {
                        var hive = ConversationsRepository.GetMergedBranchRootConversation((Guid)loaded.BranchedFromMessageId);

                        if (hive != null)
                        {
                            loaded.HiveConversation = hive;

                        }
                    }
                    Conversation = loaded;
                }
                else
                {
                    NavigationManager.NavigateTo("/conversation");
                }
            }
            else
            {
                Conversation = new Conversation
                {
                    Model = !string.IsNullOrEmpty(_modelSelector?.SelectedModel) ?_modelSelector!.SelectedModel: PipelineOptions.Value.Model!,
                    UserId = UserId
                };
                Conversation.AddMessage(new ConversationMessage(ChatMessage.FromSystem("You are a helpful assistant.")));
            }
            StateHasChanged();
        } 


        private async Task Summarize()
        {
            IsBusy = true;

            Conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser("Summarize all in 10 words")));
            await Send();

            await using var ctx = await DbContextFactory.CreateDbContextAsync();
            ctx.Attach(Conversation);
            Conversation.Summary = Conversation.Messages.Last(m => m.Role == ConversationRole.Assistant).Content;
            await ctx.SaveChangesAsync();

            await _conversations.LoadConversations();
            IsBusy = false;

        }

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private async Task SendConversation()
        {
            IsBusy = true;

            Model.Prompt = Model.Prompt?.TrimEnd('\n');

            if (!Conversation.HasStarted())
            {
                var selected = _profileSelectorStart.SelectedProfiles;
                string startMsg = string.Join(" ", selected.Select(p => p.Content));
                if (!string.IsNullOrEmpty(startMsg))
                    startMsg += "\n\n";

                Conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser(startMsg + Model.Prompt)));

                Conversation.DateStarted = DateTime.UtcNow;
                StateHasChanged();

            }
            else
            {
                Conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser(Model.Prompt!)));
                StateHasChanged();

            }

            await InterceptorHandler.Send(Conversation, inteceptorSelector?.SelectedInterceptors ?? Array.Empty<IInterceptor>());

            await Send();
                
            if (Conversation.InitStage())
            {
                var selectedEnd = _profileSelectorEnd.SelectedProfiles;
                if (selectedEnd.Any())
                {
                    foreach (var profile in selectedEnd)
                    { 
                        Console.WriteLine("QP " + profile.Content);
                        Conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser(profile.Content)));

                        await InvokeAsync(StateHasChanged);
                        await Send();

                    }
                }
            }

            IsBusy = false;
            if (selectedTabIndex == 0)
            {
                await Interop.FocusElement(_promptField2.Element);
            }
        }


        private async Task Send()
        {
            promptIsReady = false;


            var conv = Conversation;
            Conversation = conv;
            try
            {
                var stream =  Ai.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest()
                {
                    Model = Conversation?.Id != null ? Conversation.Model : _modelSelector.SelectedModel,
                    MaxTokens = 2000,
                    Temperature = 0.9f,
                    Messages = conv.Messages.Select(m => new ChatMessage(m.Role, m.Content)).ToList()

                }).ConfigureAwait(true);



                var conversationMessage = new ConversationMessage(new ChatMessage("assistant", ""));
                // add the result, with cost
                conv.AddMessage(conversationMessage);
                StateHasChanged();
                await foreach (var completion in stream)
                {
                    if (completion.Successful)
                    {
                        string? content = completion.Choices.First()?.Message.Content;
                        if (content != null)
                        {
                            conversationMessage.Content += content;
                            Console.Write(content);
                            await Task.Delay(1); // adjust the delay time as needed
                            Conversation = conv;
                            StateHasChanged();
                        }
                    }
                    else
                    {
                        if (completion.Error == null)
                        {
                            throw new Exception("Unknown Error");
                        }
                        Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
                    }
                }


                // save stuff
                StateHasChanged();

                await using var ctx = await DbContextFactory.CreateDbContextAsync();

                bool isNew = false;
                bool wasSummarized = false;

                if (conv.Id == null || conv.Id == default(Guid))
                {
                    conv.UserId = UserId;
                    conv.Model = _modelSelector!.SelectedModel!;
                    isNew = true;

                    foreach (var p in _profileSelectorStart.SelectedProfiles)
                    {
                        ctx.Attach(p);
                        conv.QuickProfiles.Add(p);
                    }
                    ctx.Conversations.Add(conv);
                }
                else
                {
                    ctx.Attach(conv);
                }


                if (conv.Summary == null)
                {
                    var last = conv.Messages.First(m => m.Role == ConversationRole.User).Content;
                    conv.Summary =
                        Model.Prompt?.Substring(0, Model.Prompt.Length >= 75 ? 75 : Model.Prompt.Length);
                    wasSummarized = true;
                }

                await ctx.SaveChangesAsync();

                conv = await InterceptorHandler.Receive(conv, inteceptorSelector?.SelectedInterceptors);

                if (wasSummarized)
                {
                    await _conversations.LoadConversations();
                    StateHasChanged();
                }


                if (isNew)
                {
                    NavigationManager.NavigateTo("/conversation/" + conv.Id, false);
                }

                StateHasChanged();

            }
            catch (TaskCanceledException )
            {
                var res = await DialogService.Alert("The operation was cancelled");
                //cancellationTokenSource = new CancellationTokenSource();
                Conversation.Messages.RemoveAt(conv.Messages.Count-1);
                StateHasChanged();
                return;
            }
            catch (Exception e)
            {
                var res = await DialogService.Alert(e.StackTrace,"An error occurred. Please try again/later. " + e.Message);
                Conversation.Messages.RemoveAt(conv.Messages.Count-1);
                Console.WriteLine(e.StackTrace);
                StateHasChanged();
                return;
            }

            Conversation = conv;
            Model.Prompt = "";
        }


        private Conversations _conversations;

        private void GoToNew()
        {
            NavigationManager.NavigateTo("/conversation", false);
        }


        BrowserWindowSize browser = new BrowserWindowSize();

        void IDisposable.Dispose()
        {
            ResizeListener.OnResized -= WindowResized;
        }

        async void WindowResized(object _, BrowserWindowSize window)
        {
            browser = window;
            StateHasChanged();
        }


        Script loadedScript;


        private async Task RunScript(Guid guid)
        {
            IsBusy = true;
            Conversation.Messages.Clear();

            loadedScript = await ScriptRepository.GetScript(guid);

            Conversation.Messages.Clear();
            Conversation.AddMessage(new ConversationMessage("system", loadedScript.SystemMessage));


            string formatted = string.Format(loadedScript.Steps.First().Message, new[] { scriptInput });
            Conversation.AddMessage(new ConversationMessage("user", formatted));
            StateHasChanged();
            await Send();

            foreach (var step in loadedScript.Steps.Skip(1))
            {
                Conversation.AddMessage(new ConversationMessage("user", step.Message));
                await Send();
                StateHasChanged();

            }
            IsBusy = false;
        }

        private string RenderType()
        {
            if (inteceptorSelector.SelectedInterceptors.Any(i => i.Name == "Structurizr Hive DSL"))
            {
                return "StructurizrDslInterceptor";
            }
            return "JsonInterceptor";
        }

        ScriptsDropdown? ScriptsDdn { get; set; }

        public bool IsBusy { get; set; }

        private bool preventDefaultKey = false;
        private async Task OnPromptKeyUp(KeyboardEventArgs obj)
        {

            if (!string.IsNullOrWhiteSpace(obj.Key))
            {
                promptIsReady = true;
            }

            if (obj.Key == "Enter" && obj.ShiftKey == false)
            {
                // move mouse focus from prompt field
                await Interop.Blurelement(_promptField2.Element);
                IsBusy = true;
                StateHasChanged();
                await SendConversation();
            }
            else
            {
                
            }
        }

        private void PromptChanged()
        {
            if (Model.Prompt?.Length > 0)
            {
                promptIsReady = true;
            }
            else
            {
                promptIsReady = false;
            }
        }


        private async Task ApplyEndProfile(QuickProfile profile)
        {
            IsBusy = true;

            Conversation.AddMessage(new ConversationMessage(ChatMessage.FromUser(profile.Content)));
            await Send();
            IsBusy = false;

        }

        [Inject]
        public TooltipService? TooltipService { get; set; }

        void ShowTooltip(ElementReference elementReference, string content, TooltipOptions? options = null)
        {
            TooltipService!.Open(elementReference, content, options);
        }


        private RadzenTextArea? _promptField2;
        private QuickProfileSelector _profileSelectorStart;

        private QuickProfileSelector _profileSelectorEnd;
        private bool useState;
        private InterceptorSelector? inteceptorSelector;
        

        private int selectedTabIndex;

 

        private async Task HiveStateClicked()
        {
            if (Interop != null)
            {
                await Interop.OpenStateViewer("hive", Conversation.Id!.ToString() ?? string.Empty, RenderType());
            }
        }
    }
}
