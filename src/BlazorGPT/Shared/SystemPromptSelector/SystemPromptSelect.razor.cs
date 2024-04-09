using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Security.Claims;

namespace BlazorGPT.Shared.SystemPromptSelector
{
    public partial class SystemPromptSelect
    {
        [Parameter]
        public bool Disabled { get; set; }

        [CascadingParameter]
        Task<AuthenticationState>? AuthenticationState { get; set; }

        string? UserId { get; set; } = null!;

        [CascadingParameter(Name = "Conversation")]
        public Conversation Conversation { get; set; }


        [Inject]
        IDbContextFactory<BlazorGptDBContext>? ContextFactory { get; set; }

        [Parameter]
        public string? InitialSystemPrompt  { get; set; }

        // export values 
        public UserSystemPrompt? SelectedPrompt { get; set; } = null;

        protected override async Task OnInitializedAsync()
        {
            if (AuthenticationState != null)
            {
                var authState = await AuthenticationState;
                var user = authState.User;
                if (user.Identity is not null && user.Identity.IsAuthenticated)
                {
                    UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                }
            }

            await using var ctx = await ContextFactory!.CreateDbContextAsync();
            Prompts = ctx.UserSystemPrompts.Where(o => o.UserId == UserId).ToList();

            var defaultPrompt = new UserSystemPrompt()
                { UserId = UserId, Name = "Default", Text = "You are a helpful assistant" };

            SelectedPrompt = Prompts.SingleOrDefault(o => o.Name == "Default", defaultPrompt);
            Conversation.SetSystemMessage(SelectedPrompt!.Text);

            grid?.Reload();
        }

        IEnumerable<UserSystemPrompt>? Prompts { get; set; }

  
        public RadzenDropDownDataGrid<UserSystemPrompt>? grid { get; set; }

        private void SelectedItemChange(object selected)
        {
            var prompt = selected as UserSystemPrompt;


            Conversation.SetSystemMessage(prompt.Text);
            StateHasChanged();
        }
    }
}
