using BlazorGPT.Pipeline;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using StackExchange.Redis;

namespace BlazorGPT.Components.Memories;

public class MemoriesService(IOptions<PipelineOptions> options, IFunctionCallingUserProvider userProvider)
{
    private readonly PipelineOptions _options = options.Value;
    private MemoryWebClient client = new MemoryWebClient(options.Value.Memory.Url, options.Value.Memory.ApiKey);

    public const string IndexDefault = "memories";

    public async Task<IEnumerable<Citation>> SearchUserDocuments(
        string? query = null,
        string? index = IndexDefault,
        double minRelevance = 0.0001,
        int docLimit = 5)
    {
        var userId = await userProvider.GetUserId();
        userId = userId.CleanKmDocumentId();

        if (string.IsNullOrWhiteSpace(query))
            query = "*";

        //if (string.IsNullOrWhiteSpace(index))
        //    index = IndexDefault;
 
        var filtered = await client.SearchAsync(query, index, minRelevance: minRelevance, limit: docLimit, filter: MemoryFilters.ByTag("user", userId));

        var ret = filtered.Results;
        foreach (var filteredResult in ret)
        {
            filteredResult.SourceUrl = _options.Memory.Url + filteredResult.SourceUrl;
        }

        return ret.OrderBy(o => o.SourceName);
    }


    public async Task<string> SaveDoc(Document doc, string? index  = IndexDefault)
    {
        //if (string.IsNullOrWhiteSpace(index))
        //    index = IndexDefault;

        var userId = await userProvider.GetUserId();
        userId = userId.CleanKmDocumentId();
        doc.AddTag("user", userId);
   
        var documentId =  await client.ImportDocumentAsync(doc, index);
        return documentId;
    }


    public async Task DeleteDoc(string documentId, string? index = IndexDefault)
    {
        await client.DeleteDocumentAsync(documentId, index);
    }

    public async Task<bool> IsDocumentReady(string documentId)
    {
        var doc = await client.IsDocumentReadyAsync(documentId, IndexDefault);
        return doc;
    }

 
    public Action <int>? OnUploadFinished;

    public Task UploadFinished(int numberOfDocs)
    {
        OnUploadFinished?.Invoke(numberOfDocs);
        return Task.CompletedTask;
    }
}

internal class FileAreaCleaner(IOptions<PipelineOptions> options)
{
    public async Task DeleteAll(string index)
    {
        await DeleteBlobs();
        await DeleteRedis(index);

    }

    // delete blob storage
    public async Task DeleteBlobs()
    {

        var blobServiceClient = new BlobServiceClient(options.Value.Memory.AzureStorageConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("smemory");
        await foreach (var blobItem in containerClient.GetBlobsAsync())
        {
            await containerClient.DeleteBlobAsync(blobItem.Name);
        }

    }
    // delete all keys in redis index
    public async Task DeleteRedis(string index)
    {
        var redis = ConnectionMultiplexer.Connect(options.Value.Memory.RedisConnectionString);
        var db = redis.GetDatabase();
        var keys = db.Execute($"KEYS", $"km-{index}*");
        var keysInDb = ((RedisResult[]?)keys).Select(o => o.ToString());
        var del = keysInDb.Select(o => new RedisKey(o)).ToArray();
        await db.KeyDeleteAsync(del, CommandFlags.None);
    }
}



public static class KernelMemoryDocumentsServiceExtensions
{
    public static  string CleanKmDocumentId(this string documentId)
    {
        documentId = documentId.Replace("\\", "");

        // replace å, ä, ö with a, a, o
        documentId = documentId.Replace("å", "a", StringComparison.InvariantCultureIgnoreCase)
            .Replace("ä", "a", StringComparison.InvariantCultureIgnoreCase)
            .Replace("ö", "o", StringComparison.InvariantCultureIgnoreCase)
            ;

       // remove all other special characters except .
        documentId = Regex.Replace(documentId, "[^a-zA-Z0-9_.]+", "_", RegexOptions.Compiled);

        return documentId;
    }
}