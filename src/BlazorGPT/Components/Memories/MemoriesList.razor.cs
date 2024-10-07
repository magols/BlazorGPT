using BlazorGPT.Pipeline;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Radzen;
using Radzen.Blazor;

namespace BlazorGPT.Components.Memories;

public partial class MemoriesList : IDisposable
{
    private List<FileMetadata> _filesInKm = new();

    private bool _hasLoadedFirstTime;

    private string _imageExtensions = ".jpeg,.jpg,.png";
    private bool _isLoading;
    
    private double _defaultRelevance = 0.5;
    private double _relevance = 0;
    private string _searchQuery = "";

    private MemoriesService _docService = null!;

    [Inject]
    public required ConversationInterop Interop { get; set; }
    [Inject] public required DialogService DialogService { get; set; }

    [Inject] public required NotificationService NotificationService { get; set; }

    [Inject] public required IServiceProvider ServiceProvider { get; set; }

    private string[] ImageExtensions => _imageExtensions.Split(',');

    public IEnumerable<Citation> CitationsInKm { get; set; } = new List<Citation>();

    RadzenDataGrid<Citation>? _dataGrid;
    private int _gridHeight = 0;

    public void Dispose()
    {
        _docService.OnUploadFinished -= UploadIsDone;
    }

    protected override async Task OnInitializedAsync()
    {
        _docService = ServiceProvider.GetRequiredService<MemoriesService>();

        _docService.OnUploadFinished += UploadIsDone;
    }

    private void UploadIsDone(int obj)
    {
        InvokeAsync(Reload);
    }


    private async Task Reload()
    {
        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            CitationsInKm = await _docService.SearchUserDocuments(_searchQuery, _relevance, 1000);
      //      await _dataGrid!.Reload();
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", e.Message);
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!CitationsInKm.Any())
                await Reload();
            _hasLoadedFirstTime = true;

            var dimensions = await Interop.GetElementDimensions("layout-body");

            //_gridHeight = 300; //dimensions.Height - 300;

            StateHasChanged();

        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private async Task DeleteFile(string documentId)
    {
        await _docService.DeleteDoc(documentId);
        Console.WriteLine($"Delete memory: {documentId}");
        await Reload();
    }

    private async Task DeleteAll()
    {
        var result = await DialogService.Confirm("Are you sure you want to delete all files?", "Delete all files",
            new ConfirmOptions { OkButtonText = "Yes", CancelButtonText = "No" });

        if (!result.HasValue || !result.Value)
            return;

        FileAreaCleaner cleaner = new(ServiceProvider.GetRequiredService<IOptions<PipelineOptions>>());

        await cleaner.DeleteAll();
        await Reload();
    }


    private async Task Clear(MouseEventArgs arg)
    {
        _searchQuery = "";
        _relevance = 0.0;
        await Reload();
    }

    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
            _relevance = 0.0;
        else
            _relevance = _relevance == 0 ? _defaultRelevance : _relevance;

        await Reload();
    }
}