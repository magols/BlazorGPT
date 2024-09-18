using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BlazorGPT.Components.FileUpload
{
    public partial class FileArea
    {

        string _fileExtensions = ".txt,.jpeg,.jpg,.png";
        string _imageExtensions = ".jpeg,.jpg,.png";
        string[] ImageExtensions => _imageExtensions.Split(',');



        [Parameter]
        public string ContainerPath { get; set; } = "Uploads";

        [Parameter]
        public string? FolderName { get; set; }

        [Parameter]
        public Func<string, IEnumerable<string>, Task>? OnSyncFiles { get; set; }

        [Inject]
        public ConversationInterop? Interop { get; set; }

        [Inject]
        public IWebHostEnvironment? WebHostEnvironment { get; set; }

        string Path => System.IO.Path.Combine(WebHostEnvironment!.WebRootPath, ContainerPath, FolderName);

        private List<FileMetadata> _uploadedFiles = new();

        private List<FileMetadata> _filesOnDisk = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Interop!.SetupFileArea();
                Interop = null;
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        protected override void OnParametersSet()
        {
            if (FolderName == null)
            {
                FolderName = Guid.NewGuid().ToString();
            }
        }


        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
            foreach (var file in e.GetMultipleFiles())
            {
                _uploadedFiles.Add(new FileMetadata
                {
                    Name = file.Name,
                    Size = file.Size,
                    LastModified = file.LastModified
                });

                var filePath = System.IO.Path.Combine(Path, file.Name);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // variable for max file size in bytes. initial value is 10MB
                var maxFileSize = 10000000;
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    await file.OpenReadStream(maxFileSize).CopyToAsync(stream);
                }


               
            }
            _model.Files = new List<IFormFile>();
            SyncFilesFromDisk();
        }

        private void SyncFilesFromDisk()
        {
            _filesOnDisk.Clear();
            if (Directory.Exists(Path))
            {
                var files = Directory.GetFiles(Path).Select(f => new FileInfo(f));
                foreach (var file in files)
                {
                     
                    _filesOnDisk.Add(new FileMetadata
                    {
                        Name = file.Name,
                        Size = file.Length,
                        LastModified = file.LastWriteTime
                    });
                }
                OnSyncFiles?.Invoke(FolderName, files.Select(f => f.Name ));

            }

            _uploadedFiles = new List<FileMetadata>();

            StateHasChanged();
        }

        private void DeleteFile(string fileName)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            SyncFilesFromDisk();
            StateHasChanged();
        }


        protected override Task OnParametersSetAsync()
        {
            SyncFilesFromDisk();
            return base.OnParametersSetAsync();
        }


        public class FileMetadata
        {
            public string? Name { get; set; } = default!;
            public long Size { get; set; }
            public DateTimeOffset LastModified { get; set; }
        }

        class FormModel
        {
            public List<IFormFile> Files { get; set; } = new();
        }

        private InputFile? _fileFormField;
        private EditForm _form;
        private FormModel _model = new();
    }
}
