namespace BlazorGPT.Components.Memories;

internal class UploadingFileStatus
{
    public int Status { get; set; }
    public string? DocumentId { get; set; }

    public FileMetadata? File { get; set; }
}