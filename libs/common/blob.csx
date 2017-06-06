#r "System.Web"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./config.csx"

using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

public static class BlobDrive
{
    private static CloudBlobContainer driveContainer = null;
    private static CloudBlobContainer driveThumbContainer = null;
    private static object lockobj = new object();

    public static string GetKey(Runtime runtime, bool isThumbnail)
    {
        Initialize();
        var container = isThumbnail ? driveThumbContainer : driveContainer;
        var adhocPolicy = new SharedAccessBlobPolicy()
        {
            Permissions = SharedAccessBlobPermissions.Read,
            SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        return container.GetSharedAccessSignature(adhocPolicy);
    }

    public static IEnumerable<IListBlobItem> List(Runtime runtime, bool useFlatBlobListing)
    {
        Initialize();
        return driveContainer.ListBlobs(useFlatBlobListing: useFlatBlobListing);
    }

    public static async Task<FileInfo> Upload(Runtime runtime, FileInfo filemeta, bool isThumbnail, System.IO.Stream fstream)
    {
        Initialize();
        var blobName = NormalizeBlobName(filemeta.FullPath);
        runtime.Log.Info("Uploading to Blob for " + filemeta.ToString());
        var blref = isThumbnail ? driveThumbContainer.GetBlockBlobReference(blobName) :
                    driveContainer.GetBlockBlobReference(blobName);
        try
        {
            await blref.UploadFromStreamAsync(fstream, new AccessCondition(), new BlobRequestOptions() { ServerTimeout = TimeSpan.FromHours(1), MaximumExecutionTime = TimeSpan.FromHours(1) }, new OperationContext());
            runtime.Log.Info("Setting Metadata for " + filemeta.ToString());
            blref.Metadata.Add("SyncId", filemeta.SyncId.ToString("D"));
            blref.Metadata.Add("Id", filemeta.Id);
            blref.Metadata.Add("Type", filemeta.Type);
            blref.Metadata.Add("MimeType", filemeta.MimeType);
            blref.Metadata.Add("Size", filemeta.Size.ToString());
            await blref.SetMetadataAsync(new AccessCondition(), new BlobRequestOptions() { ServerTimeout = TimeSpan.FromHours(1), MaximumExecutionTime = TimeSpan.FromHours(1) }, new OperationContext());
            runtime.Log.Info("Setting Properties for " + filemeta.ToString());
            blref.Properties.ContentType = filemeta.MimeType;
            await blref.SetPropertiesAsync(new AccessCondition(), new BlobRequestOptions() { ServerTimeout = TimeSpan.FromHours(1), MaximumExecutionTime = TimeSpan.FromHours(1) }, new OperationContext());
            filemeta.Blobed = true;
            return filemeta;
        }
        catch (Microsoft.WindowsAzure.Storage.StorageException)
        {
            runtime.Log.Warning("Error Detected while uploading " + filemeta);
            runtime.Log.Info("Uploading a zero byte blob block");
            var buffer = new byte[0];
            blref.UploadFromByteArray(buffer, 0, 0, new AccessCondition(), new BlobRequestOptions() { ServerTimeout = TimeSpan.FromHours(1), MaximumExecutionTime = TimeSpan.FromHours(1) }, new OperationContext());
            runtime.Log.Info("Sleep 5 seconds");
            System.Threading.Thread.Sleep(1000 * 5);
            runtime.Log.Info("Delete bad blob block");
            blref.DeleteIfExists();
            runtime.Log.Info("Success ... bad blob block removed.");
            runtime.Log.Info("Sleep 5 seconds");
            System.Threading.Thread.Sleep(1000 * 5);
            throw;
        }
        
    }

    private static string NormalizeBlobName(string blobName)
    {
        if (blobName.StartsWith("/"))
        {
            blobName = blobName.Remove(0, 1);
        }

        return blobName.Replace("%20", " ");
    }

    private static void Initialize()
    {
        if (driveContainer == null)
        {
            lock(lockobj)
            {
                if (driveContainer == null)
                {
                    var endpoint = AppConfiguration.BDriveStorage;
                    var storageAccount = CloudStorageAccount.Parse(endpoint);
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    driveContainer = blobClient.GetContainerReference(AppConfiguration.DriveContainer);
                    driveThumbContainer = blobClient.GetContainerReference(AppConfiguration.DriveThumbContainer);
                    driveContainer.CreateIfNotExists();
                    driveThumbContainer.CreateIfNotExists();
                }
            }
        }       
    }
}