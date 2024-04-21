using BlazorGPT.Data.Model;
using Microsoft.AspNetCore.Components;

namespace BlazorGPT.Web.Pages.Samples
{
    public class SamplePage : ComponentBase
    {
        public Conversation? Conversation { get; set; }

        protected Task<string> OnStreamCompletion(string s)
        {
            Conversation!.Messages.Last().Content += s;
            StateHasChanged();
            return Task.FromResult(s);
        }
    }
}
