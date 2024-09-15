using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Radzen.Blazor;

namespace BlazorGPT.Shared.SystemPromptSelector;

public partial class SystemPromptSelect
{
    const string StoreKey = "sysprompt";
    const string DefaultKey = "Default";

    private string? UserId { get; set; } = null!;

    [Parameter] public bool Disabled { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }


    [CascadingParameter(Name = "Conversation")]
    public Conversation? Conversation { get; set; }
    private Guid? _loadedId;

    [Inject] private IDbContextFactory<BlazorGptDBContext>? ContextFactory { get; set; }

    [Parameter] public string? InitialSystemPrompt { get; set; }

    public UserSystemPrompt? SelectedPrompt { get; set; } = null;

    private IEnumerable<UserSystemPrompt>? Prompts { get; set; }

    public RadzenDropDownDataGrid<UserSystemPrompt>? grid { get; set; } = null!;

    [Inject] private ILocalStorageService? LocalStorage { get; set; }

    [Inject]
    IConfiguration Configuration { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            var user = authState.User;
            if (user.Identity is not null && user.Identity.IsAuthenticated)
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        await LoadFromDb();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Conversation != null && _loadedId != Conversation.Id && Conversation.HasStarted())
        {

            SelectedPrompt = new UserSystemPrompt
            {
                Name = "",
                Text = Conversation!.GetSystemMessage()!.Content
            };

            Conversation.SetSystemMessage(Conversation.Messages.First().Content);
            StateHasChanged();
            grid?.Reload();
            _loadedId = Conversation.Id;
        }

    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || _loadedId != Conversation?.Id)
        {
            if (!Conversation.HasStarted())
            {
                await SetPromptPreference();
            }

            if (Conversation != null && SelectedPrompt != null)
            {
                Conversation.SetSystemMessage(SelectedPrompt.Text);
            }
            StateHasChanged();
            grid?.Reload();
        }
    }

    private async Task LoadFromDb()
    {
        await using var ctx = await ContextFactory!.CreateDbContextAsync();
        Prompts = ctx.UserSystemPrompts.Where(o => o.UserId == UserId).ToList();

    }

    private async Task SetPromptPreference()
    {
        var defaultPrompt = new UserSystemPrompt
            { UserId = UserId, Name = DefaultKey, Text = Configuration["PipelineOptions:DefaultSystemPrompt"] ?? "You are a helpful assistant" };

        var savedId = await LocalStorage!.GetItemAsStringAsync(StoreKey);

        if (savedId != null)
        {
            var id = Guid.Parse(savedId);
            SelectedPrompt = Prompts.SingleOrDefault(o => o.Id == id, defaultPrompt);
        }
        else
        {
            SelectedPrompt = Prompts.SingleOrDefault(o => o.Name == DefaultKey, defaultPrompt);
        }
    }

    private async Task SelectedItemChange(object selected)
    {
        var prompt = selected as UserSystemPrompt;
        if (Conversation != null) Conversation.SetSystemMessage(prompt!.Text);
        StateHasChanged();
        await LocalStorage!.SetItemAsStringAsync(StoreKey, prompt!.Id.ToString());
    }
}