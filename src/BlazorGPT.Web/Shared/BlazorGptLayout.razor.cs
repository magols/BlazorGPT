using BlazorPro.BlazorSize;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace BlazorGPT.Web.Shared;

public partial class BlazorGptLayout
{
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    [Inject]
    protected DialogService DialogService { get; set; }

    [Inject]
    protected TooltipService TooltipService { get; set; }

    [Inject]
    protected ContextMenuService ContextMenuService { get; set; }

    [Inject]
    protected NotificationService NotificationService { get; set; }

    [Inject]
    public IResizeListener ResizeListener { get; set; }

    private bool sidebarExpanded = true;

    private bool _browserIsSmall = false;

    async Task SidebarToggleClick() {
        Console.WriteLine("Clieck");
        sidebarExpanded = !sidebarExpanded;
    }
    void SidebarToggleClickMenu()
    {
        Console.WriteLine("sidelick");
        if (_browserIsSmall)
        {
            sidebarExpanded = !sidebarExpanded;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _browserIsSmall = await ResizeListener.MatchMedia(Breakpoints.SmallDown);


        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task Cliekc()
    {
        Console.WriteLine("asasas");
    }
}