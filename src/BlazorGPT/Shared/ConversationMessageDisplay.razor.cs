using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Radzen;

namespace BlazorGPT.Shared
{
    public partial class ConversationMessageDisplay
    {


        [Parameter] public bool EditMode { get; set; }

        [Parameter] public string? InitialSystemPrompt { get; set; }

        [Parameter] public required ConversationMessage Message { get; set; }

        [Parameter]
        public int MessagesCount { get; set; }

        [Parameter]
        public bool ShowTokens { get; set; }

        [Inject]
        public TooltipService? tooltipService { get; set; }

        [Inject]
        public ConversationInterop? Interop { get; set; }


        void ShowTooltip(ElementReference elementReference, string content, TooltipOptions? options = null)
        {
            tooltipService!.Open(elementReference, content, options);
        }

        [Parameter]
        public bool ShowBranches { get; set; }

        [Parameter]
        public bool ShowRestartButton { get; set; }

        [Parameter]
        public EventCallback<ConversationMessage>? OnRestartClicked { get; set; }

        private async Task RestartClicked()
        {
            if (OnRestartClicked.HasValue && OnRestartClicked.Value.HasDelegate)
            {
                await OnRestartClicked.Value.InvokeAsync(Message);
            }
        }

        [Parameter]
        public bool ShowCopyButton { get; set; } = true;

        [Parameter]
        public EventCallback<ConversationMessage>? OnCopyClicked { get; set; }

        private async Task CopyClicked()
        {
            if (OnCopyClicked.HasValue && OnCopyClicked.Value.HasDelegate)
            {
                await OnCopyClicked.Value.InvokeAsync(Message);
            }
        }

        public bool ShowShouldDisplayStateButton => Message.State?.Content != null;

        private async Task StateClicked()
        {
            if (Interop != null)
            {
                await Interop.OpenStateViewer("message", Message!.Id!.ToString() ?? string.Empty, "json");
            }
        }

        private int CalculatePromptHeight()
        {
            if (Message == null || Message?.Content == null) return 1;
            var lineBreaks = Message?.Content.Count(c => c == '\n') ?? 0;
            var rows = Message?.Content.Length / 40 + lineBreaks ?? 1;
            if (rows > 10) rows = 10;
            return rows >= 1 ? rows : 1;
        }


        public string Style =>
            GetBorder() +
            Background() +
            Color()
            ;


        private string Color()
        {
            var color = "";

            switch (Message.Role)
            {
                case "system":

                    break;
                case "assistant":

                    color = "white";
                    break;
                case "user":
                    color = "white";
                    break;
            }

            return color != "" ? $" color:{color};" : "";
        }

        private string GetBorder()
        {
            var color = BorderColor();
            if (color != "")
            {
                return @$"border-radius:6px; border: {BorderColor()} {BorderWidth()}px solid;";
            }
            return "";
        }

        private string BorderColor()
        {
            var border = "";
            switch (Message.Role)
            {
                case "system":
                    border = "#424242";
                    break;
                case "assistant":
                    border = "#424242";
                    break;
                case "user":
                    border = "#424242";
                    break;
                default:
                    border = "#262626";
                    break;
            }

            return border;
        }

        private int BorderWidth()
        {
            switch (Message.Role)
            {
                case "system":
                    return 1;
                case "assistant":
                    return 1;
                case "user":
                    return 1;
                default:
                    return 1;
            }
        }

        private string Background()
        {
            var c = "";
            switch (Message.Role)
            {
                case "system":
                    c = "#242424";
                    break;
                case "assistant":
                    c = "#303030";

                    break;
                case "user":
                    c = "#505050";
                    break;
                default:
                    return "";
            }

            return c != "" ? $" background-color:{c};" : "";
        }

        private RadzenTextArea? SystemPrompt { get; set; }

        [Inject]
        public ConversationsRepository? ConversationRepository { get; set; }

        string? _messageToEdit;

        private async Task SaveMessage()
        {
            await ConversationRepository!.UpdateMessageContent((Guid)Message!.Id!, Message.Content);
            EditMode = false;
        }

        private Task CancelEdit()
        {
            EditMode = false;
            Message!.Content = _messageToEdit!;
            StateHasChanged();
            _messageToEdit = null;
            return Task.CompletedTask;
        }


        private Task EditClicked()
        {
            _messageToEdit = Message.Content;
            EditMode = true;
            return Task.CompletedTask;
        }


        [Parameter]
        public bool ShowEditButton { get; set; }

    }
}
