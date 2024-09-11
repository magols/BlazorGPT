using Blazored.LocalStorage;
using BlazorGPT.Data.Model;
using BlazorGPT.Pages;
using BlazorGPT.Pipeline;
using BlazorGPT.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace BlazorGPT.Web.Pages.Samples
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

        [CascadingParameter]
        private Task<AuthenticationState>? AuthenticationState { get; set; }

        [Parameter]
        public Guid? ConversationId { get; set; }

        [Parameter]
        public Guid? MessageId { get; set; }

        [CascadingParameter]
        public string? UserId { get; set; }

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


    }
}
