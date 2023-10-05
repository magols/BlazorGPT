using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorGPT {


    public class ConversationInterop : IAsyncDisposable {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        public ConversationInterop(IJSRuntime jsRuntime) {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "/_content/BlazorGPT/conversationInterop.js").AsTask());
        }

        public async ValueTask<string> Prompt(string message) {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<string>("showPrompt", message);
        }

        public async ValueTask DisposeAsync() {
            if (moduleTask.IsValueCreated) {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }

        public async ValueTask ScrollToBottom(string? elementId)
        {
           var module = await moduleTask.Value;
           await module.InvokeVoidAsync("scrollToBottom", elementId);
        }

        public async ValueTask FocusElement(ElementReference elementId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusElement", elementId);
        }

        public async ValueTask Blurelement(ElementReference elementId) {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("blurElement", elementId);
        }

        public async ValueTask PreventDefaultOnEnter(ElementReference elementId)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("preventDefaultOnEnter", elementId);
        }

        public async ValueTask SetupCopyButtons()
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setupCopyButtons");
        }

        public async ValueTask OpenStateViewer(string stateType, string stateId, string renderType)
        {
            var module = await moduleTask.Value;
            var windowname = await module.InvokeAsync<string>("openStateViewer", stateType, stateId,renderType);
        }

        public async ValueTask OpenWindow(string url)
        {
            var module = await moduleTask.Value;
            var windowname = await module.InvokeAsync<string>("openWindow", url);
        }
    }

}
