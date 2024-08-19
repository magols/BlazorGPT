using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace BlazorGPT.Components;

public partial class BlazorGptLayout
{
    [Inject]
    protected IJSRuntime? JSRuntime { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    protected DialogService? DialogService { get; set; }

    [Inject]
    protected TooltipService? TooltipService { get; set; }

    [Inject]
    protected ContextMenuService? ContextMenuService { get; set; }

    [Inject]
    protected NotificationService? NotificationService { get; set; }

    [Inject]
    public IResizeListener? ResizeListener { get; set; }

    private bool sidebarExpanded = true;


    Task SidebarToggleClick()
    {
        sidebarExpanded = !sidebarExpanded;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void GoToNew()
    {
        NavigationManager.NavigateTo("/conversation", true);
    }
}
