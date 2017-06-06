#r "System.Web"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./blob.csx"
#load "./httpclient.csx"
#load "./extension.csx"
#load "./onedrive.csx"
#load "./../models/runtime.csx"
#load "./../models/onedrive.csx"
#load "./../models/fileinfo.csx"
#load "./../models/filetype.csx"

using System;
using System.Text;
using System.Web;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using System.Threading.Tasks;

public static class Sync
{
    private static List<string> SafeFolders = new List<string>() { "/Study/", "/Vehicle/", "/Sushma/", "/Personal/", "Docs", "/BellaBaby/" };
    public static  List<OneDriveItem> StartDelete(Runtime runtime, IEnumerable<OneDriveItem> allDriveFiles, DateTime timeStamp)
    {
        ulong driveSize = 0;
        foreach(var files in allDriveFiles)
        {
            driveSize += (ulong)files.Size;
        }
        runtime.Log.Info("Total drive size " + driveSize);
        var filesToDelete = new List<OneDriveItem>();
        if (driveSize > AppConfiguration.MaxAllowedSize)
        {
            var newDriveSize = driveSize;
            runtime.Log.Info("Getting Photo files");
            var pFiles = runtime.PhotoInfoMeta.Where(c => c.PartitionKey == "PhotoFiles").ToList();
            var photoFiles = pFiles.Where(c => c.TakenDateTime != default(DateTimeOffset));
            runtime.Log.Info("Photo files count : " + photoFiles.Count());
            runtime.Log.Info("Setting last modified date by photo taken date");
            foreach (var exFiles in allDriveFiles)
            {
                var photo = photoFiles.FirstOrDefault(p => p.Id == exFiles.Id);
                if (photo != null)
                {
                    exFiles.LastModifiedDateTime = photo.TakenDateTime;
                }
            }

            runtime.Log.Info("Ordering by LastModifiedTime");
            var orderedFiles = allDriveFiles.OrderBy(f => f.LastModifiedDateTime);
            runtime.Log.Info("Getting Existing blobed files");
            var existingFiles = runtime.FileInfoMeta.Where(c => c.PartitionKey == "DriveFiles" && c.Blobed).ToList();
            runtime.Log.Info("Existing files count " + existingFiles.Count);
            foreach (var files in orderedFiles)
            {
                if (SafeFolders.Any(s => files.FullPath.IndexOf(s, StringComparison.OrdinalIgnoreCase) > -1)
                    || files.FullPath.LastIndexOf("/") <= 1)
                {
                    runtime.Log.Info("Safe file " + files.FullPath);
                    continue;
                }

                //// if files is present in blobed files list then its okay to delete
                if (existingFiles.Any(e => e.Id == files.Id))
                {
                    filesToDelete.Add(files);
                    runtime.DeleteQueue.AddAsync(files);
                    newDriveSize = newDriveSize - (ulong)files.Size;
                    if (newDriveSize < AppConfiguration.SizeAfterDelete)
                    {
                        // runtime.DeleteQueue.AddAsync(files);
                   
                        /// we have reached the size to stop delete.
                        runtime.Log.Info("Expected drive size after deleting  " + newDriveSize);
                        break;
                    }
                }
                else
                {
                    runtime.Log.Info("Skip non-blobed file : " + files.FullPath);
                }
            }
        }

        runtime.Log.Info("Number of files to delete  " + filesToDelete.Count);
        var last = filesToDelete[filesToDelete.Count - 1];
        runtime.Log.Info("Last file to deleted  " + last.Type + " " + last.FullPath + " " + last.LastModifiedDateTime);
        return filesToDelete;
    }

    public static async Task<bool> SyncFile(Runtime runtime, FileInfo filetoSync)
    {
        var taskList = new List<Task>();
        runtime.Log.Info("Getting content and Thumbnail for " + filetoSync.ToString());
        var fileTask = OneDrive.GetFileContent(runtime, filetoSync.Id);
        var thumbTask = OneDrive.GetThumbnail(runtime, filetoSync.Id);
        taskList.Add(fileTask);
        taskList.Add(thumbTask);
        await Task.WhenAll(taskList);
        taskList.Clear();

        Task<FileInfo> uploadFile = null;
        Task<FileInfo> uploadThumb = null;
        if (fileTask.Result == null)
        {
            runtime.Log.Warning("No Content available for " + filetoSync.ToString());
        }
        else
        {
            runtime.Log.Info("Uploading Content for " + filetoSync.ToString());
            uploadFile = BlobDrive.Upload(runtime, filetoSync, false, fileTask.Result);
        }

        if (thumbTask.Result == null)
        {
            runtime.Log.Warning("No Thumbnail available for " + filetoSync.ToString());
        }
        else
        {
            runtime.Log.Info("Uploading Thumbnail for " + filetoSync.ToString());
            uploadThumb = BlobDrive.Upload(runtime, filetoSync, true, thumbTask.Result);
        }
       
        if (uploadFile != null)
        {
            taskList.Add(uploadFile);
        }

        if (uploadThumb != null)
        {
            taskList.Add(uploadThumb);
        }
       
        await Task.WhenAll(taskList);
        if (uploadFile != null && uploadFile.Result.Blobed)
        {
            runtime.Log.Info("Marking Blobed " + filetoSync.ToString());
            var operation = TableOperation.InsertOrReplace(filetoSync);
            await runtime.FileInfoTable.ExecuteAsync(operation);
        }
        else
        {
            runtime.Log.Warning("Marking Blobed Failed for " + filetoSync.ToString());
        }

        return true;
    }

    public static async Task<SyncInfo> Start(Runtime runtime,  IEnumerable<OneDriveItem> allDriveFiles, DateTime timeStamp)
    {
        var syncId = Guid.NewGuid();
        runtime.Log.Info("Sync started with id : " + syncId.ToString("D"));
        var allFiles = new List<FileInfo>();
        foreach (var d in allDriveFiles)
        {
            if (!d.IsDeleted)
            {
                allFiles.Add(new FileInfo(syncId, d, timeStamp));
            }
        }
          
        var newFiles = GetNewFiles(runtime, allFiles);       
        var newFileCount = 0;
        ulong totalSize = 0;
        if (newFiles.Any())
        {
            int batchCount = 0;
            var batchOperation = GetOperations();
            var duplicates = new Dictionary<string, FileInfo>();
            var errorList = new List<string>();
            /*
            var photoMap = new Dictionary<int, FileInfo>();
            var audioMap = new Dictionary<int, FileInfo>();
            var videoMap = new Dictionary<int, FileInfo>();
            var locationMap = new Dictionary<int, FileInfo>();
            var fileMap = new Dictionary<int, FileInfo>();
            var photoCounter = 0;
            var audioCounter = 0;
            var videoCounter = 0;
            var locationCounter = 0;
            var errorEntities = new List<FileInfo>(); 
            */
            foreach (var nfile in newFiles)
            {
                if (duplicates.ContainsKey(nfile.Id))
                {
                    runtime.Log.Error("Duplicate: New Record : " + nfile.ToString());
                    var erec = duplicates[nfile.Id];
                    runtime.Log.Error("Duplicate: Existing Record : " + erec.ToString());
                }
                else
                {
                    newFileCount++;
                    duplicates.Add(nfile.Id, nfile);
                    totalSize = totalSize + (ulong)nfile.Size;
                    var dfile = allDriveFiles.FirstOrDefault(a => a.Id == nfile.Id);
                    if (dfile == null)
                    {
                        var err = "Id not found in alldrivefiles: " + nfile.ToString();
                        errorList.Add(err);   
                    }
                    else
                    {
                        switch (dfile.Type)
                        {
                            case FileType.Photo:
                                batchOperation[0].InsertOrReplace(new PhotoInfo(syncId, dfile, timeStamp));
                                /*
                                photoMap.Add(photoCounter, nfile);
                                photoCounter++;
                                */
                                break;
                            case FileType.Audio:
                                batchOperation[1].InsertOrReplace(new AudioInfo(syncId, dfile, timeStamp));
                                /*
                                audioMap.Add(audioCounter, nfile);
                                audioCounter++;
                                */
                                break;
                            case FileType.Video:
                                batchOperation[2].InsertOrReplace(new VideoInfo(syncId, dfile, timeStamp));
                                /*
                                videoMap.Add(videoCounter, nfile);
                                videoCounter++;
                                */
                                break;
                        }

                        if (dfile.Location != null)
                        {
                            batchOperation[3].InsertOrReplace(new LocationInfo(syncId, dfile, timeStamp));
                            /*
                            locationMap.Add(locationCounter, nfile);
                            locationCounter++;
                            */
                        }

                        batchOperation[4].InsertOrReplace(nfile);
                        //  fileMap.Add(batchCount, nfile);
                        batchCount++;

                        if (batchCount == TabkeBatchOperationSize)
                        {
                            runtime.Log.Info("Sync Batch Count hit limit of " + TabkeBatchOperationSize + " items... Flushing...");
                            /*
                            for (int i = 0; i < batchOperation.Length; i++)
                            {
                                var op = batchOperation[i];
                                if (op.Count > 0)
                                {
                                    try
                                    {
                                        runtime.FileInfoTable.ExecuteBatch(op);
                                    }
                                    catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                                    {
                                        var message = ex.Message.Replace("Element ", string.Empty).Replace(" in the batch returned an unexpected response code.", string.Empty);
                                        runtime.Log.Error("Error at : " + message);
                                        var batchIndex = Int32.Parse(message);
                                        FileInfo errorEntity = null;
                                        switch (i)
                                        {
                                            case 0:
                                                errorEntity = photoMap[batchIndex];
                                                break;
                                            case 1:
                                                errorEntity = audioMap[batchIndex];
                                                break;
                                            case 2:
                                                errorEntity = videoMap[batchIndex];
                                                break;
                                            case 3:
                                                errorEntity = locationMap[batchIndex];
                                                break;
                                            case 4:
                                                errorEntity = fileMap[batchIndex];
                                                break;
                                        }

                                        if (errorEntity != null)
                                        {
                                            errorEntities.Add(errorEntity);
                                        }
                                    }
                                }
                            }
                            */

                            var taskList = new List<Task>();
                            foreach (var op in batchOperation)
                            {
                                if (op.Count > 0)
                                {
                                    taskList.Add(runtime.FileInfoTable.ExecuteBatchAsync(op));
                                }
                            }

                            try
                            {
                                await Task.WhenAll(taskList);
                            }
                            catch (Microsoft.WindowsAzure.Storage.StorageException ex)
                            {
                                errorList.Add("Error on Execute Batch Async : " + ex.Message);
                            }

                            /*
                            photoCounter = 0;
                            audioCounter = 0;
                            videoCounter = 0;
                            locationCounter = 0;
                            photoMap.Clear();
                            audioMap.Clear();
                            videoMap.Clear();
                            locationMap.Clear();
                            fileMap.Clear();
                            */

                            batchCount = 0;
                            batchOperation = GetOperations();
                            runtime.Log.Info("Saved " + newFileCount + " items to DriveFiles.");

                        }

                        //// push to Queue
                        // if (newFileCount < 5)
                        // {
                            runtime.OutputQueue.AddAsync(nfile);
                        //}
                    }
                }
            }

            if (errorList.Count > 0)
            {
                foreach (var eitem in errorList)
                {
                    runtime.Log.Warning(eitem);
                }
                runtime.Log.Warning("Total Errored items : " + errorList.Count.ToString());
            }
           
            runtime.Log.Info("All files synced to DriveFiles.");         
        }

        runtime.Log.Info("Done with sync.. updating syncinfo");
        var elapsed = DateTime.UtcNow - timeStamp;
        runtime.Log.Info("Total files : " + newFileCount);
        runtime.Log.Info("Total size : " + totalSize);
        runtime.Log.Info("Sync duration : " + elapsed);
        var syncInfo = new SyncInfo
        {            
            Duration = elapsed,
            Count = newFileCount,
            Size = totalSize,
            Timestamp = timeStamp,
            PartitionKey = "SyncInfo",            
            RowKey = syncId.ToString("D"),
            ETag = "*"
        };
      
        var operation = TableOperation.InsertOrReplace(syncInfo);
        await runtime.SyncInfoTable.ExecuteAsync(operation);
        runtime.Log.Info("Syncinfo updated");
        return syncInfo;
    }

    private static TableBatchOperation[] GetOperations()
    {
        var batchOperation = new TableBatchOperation[5];
        batchOperation[0] = new TableBatchOperation();
        batchOperation[1] = new TableBatchOperation();
        batchOperation[2] = new TableBatchOperation();
        batchOperation[3] = new TableBatchOperation();
        batchOperation[4] = new TableBatchOperation();
        return batchOperation;
    }   

    private static IEnumerable<FileInfo> GetNewFiles(Runtime runtime, IEnumerable<FileInfo> allFiles)
    {
        runtime.Log.Info("All files Count : " + allFiles.Count());
        var existingFiles = runtime.FileInfoMeta.Where(c => c.PartitionKey == "DriveFiles").ToList();
        runtime.Log.Info("Existing files Count : " + existingFiles.Count);
        var newFiles = allFiles.Except(existingFiles, new FileInfoComparer()).ToList();
        runtime.Log.Info("Except files Count : " + newFiles.Count);
        var nonBlobedFiles = existingFiles.Where(b => !b.Blobed);
        runtime.Log.Info("Non blobed files Count : " + nonBlobedFiles.Count());
        newFiles.AddRange(nonBlobedFiles);
       runtime.Log.Info("Final New file Count : " + newFiles.Count);
       return newFiles;
    }

}