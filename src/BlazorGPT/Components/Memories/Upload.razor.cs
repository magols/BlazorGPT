using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Radzen;

namespace BlazorGPT.Components.Memories;
public partial class Upload
{
    [Inject] public ConversationInterop? Interop { get; set; }

    [Inject] public required NotificationService NotificationService { get; set; }

    [Inject] public required IServiceProvider ServiceProvider { get; set; }


    private string _index = MemoriesService.IndexDefault;
    [Parameter]
    public string? Index
    {
        get => _index;
        set
        {
            if (value != null)
            {
                _index = value;
            }
        }
    }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string _fileExtensions = ".txt,.md,.pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx";
    //string _fileExtensions = ".txt,.md,.pdf,.jpeg,.jpg,.png";

    private string _imageExtensions = ".jpeg,.jpg,.png";
    private string[] ImageExtensions => _imageExtensions.Split(',');

    private EditForm _form = new();
    private FormModel _model = new();
    private List<UploadingFileStatus> _filesToUpload = [];

    private MemoriesService docService;

    protected override async Task OnInitializedAsync()
    {
        docService = ServiceProvider.GetRequiredService<MemoriesService>();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Interop!.SetupFileArea();
            Interop = null;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        IReadOnlyList<IBrowserFile>? files;
        try
        {
            files = e.GetMultipleFiles(100);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Error", ex.Message);
            return;
        }

        _filesToUpload = new List<UploadingFileStatus>();
        _filesToUpload.AddRange(files.Select(f =>
            new UploadingFileStatus
            {
                DocumentId = f.Name.CleanKmDocumentId(),
                File = new FileMetadata
                {
                    DocumentId = f.Name.CleanKmDocumentId(),
                    Name = f.Name,
                    Size = f.Size,
                    LastModified = f.LastModified
                },
                Status = 0
            }
        ));


        foreach (var file in files)
        {
            var documentId = file.Name.CleanKmDocumentId();
            var doc = new Document(documentId);
            var target = _filesToUpload.First(o => o.File.Name == file.Name);
            try
            {
                // max bytes allowed in the server api is 30000000 bytes
                var maxLength = 30000000;
                var fileStream = file.OpenReadStream(maxLength);
                var ms = new MemoryStream();
                await fileStream.CopyToAsync(ms);
                doc.AddStream(file.Name, ms);
                await docService.SaveDoc(doc, _index);
                target.Status = 1;
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception exception)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error uploading " + file.Name,
                    exception.Message);
                target.Status = 3;
                await InvokeAsync(StateHasChanged);
            }
        }

        NotificationService.Notify(NotificationSeverity.Success, "Upload", (files.Count > 1 ? "Files" : "File") + " uploaded");

        await CheckUploadStatus();
    }

    private async Task CheckUploadStatus()
    {
        await Task.Run(async () =>
        {
            var howMany = _filesToUpload.Count(o => o.Status <= 1);
            while (_filesToUpload.Any(o => o.Status <= 1))
            {
                foreach (var file in _filesToUpload.Where(o => o.Status == 1))
                {
                    try
                    {
                        var isReady = await docService.IsDocumentReady(file.DocumentId);
                        if (isReady)
                        {
                            file.Status = 2;
                            await InvokeAsync(StateHasChanged);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        file.Status = 3;
                    }

                    await InvokeAsync(StateHasChanged);
                }

                await Task.Delay(800);
            }

            await docService.UploadFinished(howMany);
            NotificationService.Notify(NotificationSeverity.Success, "Upload", (howMany > 1 ? "Files" : "File") + " ingested");

        });
    }

    private int UploadProgress => (int)(_filesToUpload.Count(o => o.Status == 2) / (double)_filesToUpload.Count() * 100);

    private string GetFileSizeHumanFriendly(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} bytes";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / 1024 / 1024} MB";
        return $"{bytes / 1024 / 1024 / 1024} GB";
    }

    #region styling

    // render an icon basd on the file extension
    public static string GetIconByExtension(string filename)
    {
        var extension = Path.GetExtension(filename).ToLower();
        switch (extension)
        {
            case ".pdf":
                return "picture_as_pdf";
            case ".md":
                return "markdown";
            case ".txt":
                return "description";
            default:
                return "draft";
        }
    }

    // get icon by status 0 = draft, 1 = uploaded, 2 = ingested
    private string GetIconByStatus(int status)
    {
        switch (status)
        {
            case 0:
                return "cloud_sync";
            case 1:
                return "cloud_upload";
            case 2:
                return "cloud_done";
            case 3:
                return "cloud_upload";
            default:
                return "cloud";
        }
    }

    private string GetColorByStatus(int status)
    {
        switch (status)
        {
            case 0:
                return "gray";
            case 1:
                return "lightblue";
            case 2:
                return "green";
            case 3:
                return "red";
            default:
                return "black";
        }
    }

    #endregion

    private class FormModel
    {
        public List<IFormFile> Files { get; set; } = new();
    }
}
