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

    public static async Task<FileInfo> Upload(Runtime runtime, FileInfo filemeta, bool isThumbnail, System.IO.Stream fstream)
    {
        Initialize();
        var blobName = NormalizeBlobName(filemeta.FullPath);
        runtime.Log.Info("Uploading to Blob for " + filemeta.ToString());
        var blref = isThumbnail ? driveThumbContainer.GetBlockBlobReference(blobName) :
                    driveContainer.GetBlockBlobReference(blobName);
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
        runtime.Log.Info("File Blobed: " + filemeta.ToString());
        return filemeta;
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