using Blazored.LocalStorage;
using BlazorGPT.Pipeline;
using BlazorGPT.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BlazorGPT.Pages
{
    public abstract class SamplePage : ComponentBase
    {
        public Conversation? Conversation { get; set; }

        protected Task<string> OnStreamCompletion(string s)
        {
            Conversation!.Messages.Last().Content += s;
            StateHasChanged();
            return Task.FromResult(s);
        }

        protected async Task SetUser() => UserId = UserId ?? await UserStorage.GetUserIdFromLocalStorage();


        protected ConversationPage? ConversationPage;

        [Parameter]
        public Guid? ConversationId { get; set; }

        [Parameter]
        public Guid? MessageId { get; set; }

        [Inject]
        public IConfiguration? Configuration { get; set; }

        [Inject]
        public NavigationManager? NavigationManager { get; set; }

        [Inject]
        public required IOptions<PipelineOptions> PipelineOptions { get; set; }

        [Inject]
        public required PluginsConfigurationService PluginsConfigurationService { get; set; }

        [Inject]
        public required InterceptorConfigurationService InterceptorConfigurationService { get; set; }

        [Inject]
        public required ModelConfigurationService ModelConfigurationService { get; set; }

        [Inject]
        public required ILocalStorageService LocalStorage { get; set; }

        [Inject]
        public required UserStorageService UserStorage { get; set; }

        protected string NewPath;

        protected void GoToNew()
        {
            NavigationManager!.NavigateTo(NewPath, false);

        }


        [CascadingParameter]
        private Task<AuthenticationState>? authenticationState { get; set; }
        public string? UserId { get; set; } = null!;


        protected override async Task OnInitializedAsync()
        {
            var authState = await authenticationState;
            var user = authState?.User;
            if (user?.Identity is not null && user.Identity.IsAuthenticated)
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await SetUser();
            }
        }
    }
}
