using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using Blazored.LocalStorage;
using BlazorGPT.Pipeline;
using BlazorGPT.Pipeline.Interceptors;
using BlazorGPT.Settings;
using BlazorGPT.Shared;
using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Radzen;
using Radzen.Blazor;

namespace BlazorGPT.Pages
{
    public partial class ConversationPage : IDisposable
    {
        private bool _browserIsSmall = true;

        private async Task CancelSend()
        {
            await _cancellationTokenSource.CancelAsync();
            
        }

        [Inject]
        public NotificationService NotificationService { get; set; } = null!;

        private async Task RestartConversation(ConversationMessage msg)
        {
            var result = await DialogService.Confirm("Restart conversation from here?", "Restart");
            if (result.HasValue && result.Value)
            {
                var isUserMsg = msg.Role == "user";
                if (isUserMsg)
                {
                    Model.Prompt = msg.Content.Trim();
                }

                var messageCutoff = Conversation.Messages.IndexOf(msg) + 1;

                var messagesToRemove = Conversation.Messages.Skip(messageCutoff).ToList();
                await ConversationsRepository.DeleteMessages(messagesToRemove);

                Conversation.Messages = Conversation.Messages.Take(messageCutoff).ToList();

                var lastUserMessage = Conversation.Messages.FindLast(o => o.Role == ConversationRole.User)!;
                lastUserMessage.ActionLog = null;

                await SendConversation(rerun: true);

                StateHasChanged();

                NotificationService.Notify(NotificationSeverity.Success, "Conversation restarted");
            }
        }



        private async Task StateClicked()
        {
            if (Interop != null)
            {
                var srs = await RenderType();
                await Interop.OpenStateViewer("conversation", Conversation.Id!.ToString() ?? string.Empty, srs);
            }
        }

        private async Task Copy(ConversationMessage msg, string newName)
        {
            var newConvo = await ConversationsRepository.BranchFromMessage(UserId, msg, newName, Conversation);
            NavigationManager.NavigateTo("/conversation/" + newConvo.Id);
            await _conversations.LoadConversations();
            NotificationService.Notify(NotificationSeverity.Success, "Conversation branched");
        }

        private string ConversationDisplayStyle()
        {
            if (browser.Height == 0 || controlHeight == 0)
            {
                return "";
            }
            var height = browser.Height - controlHeight;
            return $"height: {height}px";
        }

        private async Task ShareClicked()
        {
            await Interop.OpenWindow("/share/" + ConversationId);
        }


        public class PromptModel
        {
            [Required] public string? Prompt { get; set; } = "";
        }

        [Parameter] public bool ShowActionLog { get; set; } = true;

        [Parameter]
        public bool UseFileUpload { get; set; }


        [Parameter]
        public string? NewDestinationPrefix { get; set; }

        [Parameter]
        public string? Class { get; set; }

        [Parameter]
        public string? Style { get; set; }
        [Parameter]
        public RenderFragment? ChildContent{ get; set; }

        [Parameter]
        public RenderFragment? ButtonContent { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState>? AuthenticationState { get; set; }

        [Parameter]
        [SupplyParameterFromQuery]
        public bool BotMode { get; set; }

        [Parameter] public string? BotSystemInstruction { get; set; } = null;

        [Parameter]
        public string? UserId { get; set; } = null!;

        [Parameter]
        public Guid? ConversationId { get; set; }

        private Guid _loadedConversationId = default;

        [Parameter]
        public Guid? MessageId { get; set; }

        PromptModel Model = new();

        [Inject]
        public IOptions<PipelineOptions> PipelineOptions { get; set; } = null!;

        [Inject]
        public required QuickProfileRepository QuickProfileRepository { get; set; }

        [Inject]
        public required IResizeListener ResizeListener { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = null!;
        
        [Inject]
        public required KernelService KernelService{ get; set; }

        [Inject]
        public required ILoggerFactory LoggerFactory { get; set; }
        private ILogger<ConversationPage> _logger;

        [Inject]
        public IDbContextFactory<BlazorGptDBContext> DbContextFactory { get; set; } = null!;
        [Inject]
        public ScriptRepository ScriptRepository { get; set; } = null!;

        [Inject]
        public required ConversationsRepository ConversationsRepository { get; set; }
        [Inject]
        public DialogService DialogService { get; set; } = null!;

        [Inject]
        public required IInterceptorHandler  InterceptorHandler{ get; set; }

        [Inject]
        public required ConversationInterop Interop { get; set; }
       
        public Conversation Conversation = new();

        private ModelConfiguration? _modelConfiguration;

        bool promptIsReady;
        string scriptInput;

        private int controlHeight { get; set; }
        private int initialControlHeight = 0;

        private Kernel _kernel = null!;
        private CancellationTokenSource _cancellationTokenSource = null!;
        SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

        [Inject] public required ModelConfigurationService _modelConfigurationService { get; set; }

         protected override async Task OnInitializedAsync()
         {
             _logger = LoggerFactory.CreateLogger<ConversationPage>();

            if (UserId == null && AuthenticationState != null)
            {
                var authState = await AuthenticationState;
                var user = authState?.User;
                if (user?.Identity is not null && user.Identity.IsAuthenticated)
                {
                    UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                }
            }

            BotSystemInstruction ??= PipelineOptions.Value.Bot.BotSystemInstruction;

            InterceptorHandler.OnUpdate += UpdateAndRedraw;
         }

         private async Task  UpdateAndRedraw()
        {
            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnParametersSetAsync()
        {
            UseFileUpload = BotMode ? PipelineOptions!.Value.Bot.FileUpload.Enabled :
                    PipelineOptions!.Value.FileUpload.Enabled;
        }



        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (BotMode && UserId == null) UserId = await UserStorage.GetUserIdFromLocalStorage();

            }

            if ((ConversationId == null && _loadedConversationId == default)
                || (ConversationId != _loadedConversationId ))
            {
                await SetupConversation();
            }


            if (firstRender)
            {
                ResizeListener.OnResized += WindowResized;
                _browserIsSmall = await ResizeListener.MatchMedia(Breakpoints.SmallDown);
                initialControlHeight = _browserIsSmall ? 335 : 335;
                initialControlHeight = BotMode ? 200 : initialControlHeight;
                controlHeight = initialControlHeight;

                await Interop.SetupCopyButtons();

                await Interop.ScrollToBottom("layout-body");
                await Interop.ScrollToBottom("message-pane");
            }

            if (! _browserIsSmall && (BotMode || selectedTabIndex == 0))
            {
                await Interop.FocusElement(_promptField2.Element);
            }
        }

        async Task SetupConversation()
        {
            if (ConversationId == null)
            {
                Conversation = CreateDefaultConversation();
                ConversationId = Guid.Empty;
                _loadedConversationId = Guid.Empty;
                StateHasChanged();
                return;
            }

            if (ConversationId != _loadedConversationId)
            {
                var loaded = await ConversationsRepository.GetConversation(ConversationId);
                if (loaded != null)
                {
                    _loadedConversationId = loaded.Id ?? default;
                    if (loaded.UserId != UserId)
                    {
                        throw new UnauthorizedAccessException();
                    }
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
 
            StateHasChanged();
        }

        [Inject]
        public required UserStorageService UserStorage { get; set; }

        public async Task SendMessage(string message, string role = "user")
        {
            if (role == "user")
            {
                Model.Prompt += message;
                await SendConversation();
            }
            else
            {
                throw new NotImplementedException("Other types of messages than user is not supported");
            }
        }

        private async Task SendConversation()
        {
            await SendConversation(false);
        }


        private async Task SendConversation(bool rerun)
        {
            if (IsBusy) return;

            IsBusy = true;

            _cancellationTokenSource = new CancellationTokenSource(5 * 60 * 1000);

            _modelConfiguration = await _modelConfigurationService.GetConfig();
            _kernel = await KernelService.CreateKernelAsync(provider: _modelConfiguration.Provider, model: _modelConfiguration!.Model);

            var interceptorKeyExists = await LocalStorageService.ContainKeyAsync("bgpt_interceptors");
            var interceptorNames = interceptorKeyExists ? await LocalStorageService.GetItemAsync<List<string>>("bgpt_interceptors") : [];



            Model.Prompt = Model.Prompt?.TrimEnd('\n');

            if (!Conversation.HasStarted())
            {
                var selected = _profileSelectorStart != null ? _profileSelectorStart.SelectedProfiles : new List<QuickProfile>();
                
                string startMsg = string.Join(" ", selected.Select(p => p.Content));
                if (!string.IsNullOrEmpty(startMsg))
                    startMsg += "\n\n";

                if (!rerun)
                {
                    Conversation.AddMessage(new ConversationMessage("user", startMsg + Model.Prompt!));
                    Conversation.DateStarted = DateTime.UtcNow;
                }
            
            }
            else if (!rerun)
            {
                Conversation.AddMessage(new ConversationMessage("user", Model.Prompt!));
             
            }
            StateHasChanged();
            await Interop.ScrollToBottom("message-pane");

            try
            {
                var c = Conversation;
                Conversation = await InterceptorHandler.Send(_kernel,
                    Conversation,
                    enabledInterceptors: null,
                    enabledInterceptorNames: interceptorNames,
                    OnStreamCompletion,
                    cancellationToken: _cancellationTokenSource.Token);

                await Send();

                if (Conversation.InitStage())
                {
                    var selectedEnd = _profileSelectorEnd != null
                        ? _profileSelectorEnd.SelectedProfiles
                        : new List<QuickProfile>();
                    if (selectedEnd.Any())
                        foreach (var profile in selectedEnd)
                        {
                            Conversation.AddMessage(new ConversationMessage("user", profile.Content));


                            StateHasChanged();
                            await Send();
                        }
                }
            }

            catch (OperationCanceledException)
            {
                var res = await DialogService.Alert("The operation was cancelled");
                Conversation.Messages.RemoveAt(Conversation.Messages.Count - 1);
            }

            catch (Exception e)
            {
                var res = await DialogService.Alert(e.StackTrace,
                    "An error occurred. Please try again/later. " + e.Message);
                Conversation.Messages.RemoveAt(Conversation.Messages.Count - 1);
            }
            finally
            {
                semaphoreSlim.Release();
            }
   

            IsBusy = false;
            if (selectedTabIndex == 0)
            {
                await Interop.FocusElement(_promptField2.Element);
            }
            StateHasChanged();
            await Interop.ScrollToBottom("message-pane");

        }

        private async Task Send()
        {
            try
            {
                if (!Conversation.StopRequested)
                {
                    _modelConfiguration ??= await _modelConfigurationService.GetConfig();
                    
                    Conversation.AddMessage("assistant", "");
                    StateHasChanged();
                    await Interop.ScrollToBottom("message-pane");


                    var chatRequestSettings = new ChatRequestSettings();
                    chatRequestSettings.ExtensionData["max_tokens"] = _modelConfiguration!.MaxTokens;
                    chatRequestSettings.ExtensionData["temperature"] = _modelConfiguration!.Temperature;

                    Conversation = await
                        KernelService.ChatCompletionAsStreamAsync(_kernel, Conversation, chatRequestSettings, OnStreamCompletion, cancellationToken: _cancellationTokenSource.Token);

                }


                await using var ctx = await DbContextFactory.CreateDbContextAsync();

                bool isNew = false;
                bool wasSummarized = false;

                if (Conversation.Id == null || Conversation.Id == default(Guid))
                {
                    Conversation.UserId = UserId;
                    Conversation.Model = _modelConfiguration!.Model!;
                    isNew = true;

                    if (_profileSelectorStart != null)
                    {
                        foreach (var p in _profileSelectorStart.SelectedProfiles)
                        {
                            ctx.Attach(p);
                            Conversation.QuickProfiles.Add(p);
                        }
                    }

                    ctx.Conversations.Add(Conversation);
                }
                else
                {
                    ctx.Attach(Conversation);
                }


                if (Conversation.Summary == null)
                {
                    var last = Conversation.Messages.First(m => m.Role == ConversationRole.User).Content;
                    Conversation.Summary =
                        Model.Prompt?.Substring(0, Model.Prompt.Length >= 75 ? 75 : Model.Prompt.Length);
                    wasSummarized = true;
                }

                await ctx.SaveChangesAsync();

                Conversation =
                    await InterceptorHandler.Receive(_kernel, Conversation,
                       enabledInterceptorNames: await LocalStorageService.GetItemAsync<List<string>>("bgpt_interceptors"));

                if (!BotMode  && wasSummarized)
                {
                    await _conversations.LoadConversations();
                    StateHasChanged();
                }
                

                if (isNew)
                {
                    NavigationManager.NavigateTo(
                        (BotMode || !string.IsNullOrEmpty(NewDestinationPrefix)) ? NewDestinationPrefix + "/" + Conversation.Id
                                : "/conversation/" + Conversation.Id, 
                        false);
                }

                StateHasChanged();

            }
            catch (OperationCanceledException)
            {
                var res = await DialogService.Alert("The operation was cancelled");
                Conversation.Messages.RemoveAt(Conversation.Messages.Count - 1);
            }
            catch (Exception e)
            {
                var res = await DialogService.Alert(e.StackTrace,
                    "An error occurred. Please try again/later. " + e.Message);
                Conversation.Messages.RemoveAt(Conversation.Messages.Count - 1);
            }
            finally
            {
                IsBusy = false;
                semaphoreSlim.Release();

            }

            Model.Prompt = "";
            promptIsReady = false;
        }

        private async Task<string> OnStreamCompletion(string s)
        {
    

            Conversation.Messages.Last().Content += s; 
            await Interop.ScrollToBottom("message-pane");

            StateHasChanged();
            return s;
        }

        private Conversations _conversations;

        BrowserWindowSize browser = new BrowserWindowSize();

        void IDisposable.Dispose()
        {
            ResizeListener.OnResized -= WindowResized;

            if (InterceptorHandler != null)
                InterceptorHandler.OnUpdate -= UpdateAndRedraw;
        }

         void WindowResized(object _, BrowserWindowSize window)
        {
            browser = window;
            StateHasChanged();
        }


        Script loadedScript;


        private async Task RunScript(Guid guid)
        {
            IsBusy = true;
            Conversation.Messages.Clear();

            _modelConfiguration = await _modelConfigurationService.GetConfig();
            _kernel = await KernelService.CreateKernelAsync(_modelConfiguration.Provider, _modelConfiguration.Model);

            loadedScript = await ScriptRepository.GetScript(guid);

            Conversation.Messages.Clear();
            Conversation.AddMessage(new ConversationMessage("system", loadedScript.SystemMessage));


            string formatted = string.Format(loadedScript.Steps.First().Message, new[] { scriptInput });
            Conversation.AddMessage(new ConversationMessage("user", formatted));
            StateHasChanged();

            _cancellationTokenSource = new CancellationTokenSource(5 * 60 * 1000);

            await Send();

            foreach (var step in loadedScript.Steps.Skip(1))
            {
                Conversation.AddMessage(new ConversationMessage("user", step.Message));
                await Send();
                StateHasChanged();

            }
            IsBusy = false;
        }

        private async Task<string> RenderType()
        {
            var enabled = await LocalStorageService.GetItemAsync<List<string>>("bgtpc_interceptors");
            if (enabled.Any(i => i == "Structurizr Hive DSL"))
            {
                return "StructurizrDslInterceptor";
            }
            return "JsonInterceptor";
        }

        ScriptsDropdown? ScriptsDdn { get; set; }

        public bool IsBusy { get; set; }
        public bool SendButtonDisabled => SendDisabled();

        public bool SendDisabled()
        {
            if (IsBusy)
            {
                return true;
            }


            if (!Conversation.IsStarted() && _profileSelectorStart != null && _profileSelectorStart.SelectedProfiles.Any())
            {
                return false;
            }

            if (promptIsReady) return false;

            return true;

        }

        private bool preventDefaultKey = false;

        private async Task OnPromptInput(ChangeEventArgs args)
        {
            promptIsReady = !string.IsNullOrEmpty(args.Value?.ToString());
        }

        private async Task OnPromptKeyUp(KeyboardEventArgs obj)
        {
            if (obj.Key == "Enter" && obj.ShiftKey == false)
            {
                // move mouse focus from prompt field
                await Interop.Blurelement(_promptField2.Element);
                IsBusy = true;
                StateHasChanged();
                await SendConversation();
            }
        }

        private async Task StartProfileClicked(QuickProfile profile)
        {
            await InvokeAsync(StateHasChanged);
        }


        private async Task ApplyEndProfile(QuickProfile profile)
        {
            IsBusy = true;
            _modelConfiguration = await _modelConfigurationService.GetConfig();
            _kernel = await KernelService.CreateKernelAsync(_modelConfiguration.Provider, _modelConfiguration.Model);
            Conversation.AddMessage(new ConversationMessage("user", profile.Content));
            _cancellationTokenSource = new CancellationTokenSource(5 * 60 * 1000);
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
        private QuickProfileSelector? _profileSelectorStart;

        private QuickProfileSelector? _profileSelectorEnd;

        [Inject]
        ILocalStorageService LocalStorageService { get; set; } = null!;
        
        private int selectedTabIndex;

        private async Task HiveStateClicked()
        {
            if (Interop != null)
            {
                await Interop.OpenStateViewer("hive", Conversation.Id!.ToString() ?? string.Empty, await RenderType());
            }
        }


        private Conversation CreateDefaultConversation()
        {
            var c = new Conversation
            {
                Model = !string.IsNullOrEmpty(_modelConfiguration?.Model) ? _modelConfiguration!.Model : PipelineOptions.Value.Providers.OpenAI.ChatModel!,
                UserId = UserId
            };

            var message = new ConversationMessage("system", "You are a helpful assistant.");
            if (BotMode) message.Content = BotSystemInstruction!;

            c.AddMessage(message);
            return c;
        }

        private async Task FileAreaSynced(string folderId, IEnumerable<string> files)
        {
            Conversation.FileUrls = files.ToList();

            bool uploadIsCreatingNewConveration = Conversation.Id == null;
            if (uploadIsCreatingNewConveration)
            {
                Conversation = CreateDefaultConversation();

                Conversation.Id = Guid.Parse(folderId);
                await ConversationsRepository.SaveConversation(Conversation);
            }
            else
            {
                await ConversationsRepository.UpdateConversation(Conversation);
            }

            if (uploadIsCreatingNewConveration)
            {
                NavigationManager.NavigateTo(
                    BotMode ? NewDestinationPrefix + "/" + Conversation.Id
                        : "/conversation/" + Conversation.Id,
                    false);
            }

        }
    }
}
