#r "System.Web"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "./constants.csx"
#load "./extension.csx"
#load "./httpclient.csx"
#load "./config.csx"
#load "./oauth.csx"
#load "./../models/onedrive.csx"
#load "./../models/onedriveerrorresponse.csx"

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
using System.Threading.Tasks;

public static class OneDrive
{

    private static int FolderCount = 0;

    public static async Task<List<OneDriveItem>> GetAllFiles(Runtime runtime)
    {        
        FolderCount = 0;
        var fileList = await GetFilesAndFolders(runtime, new OneDriveItem { Id = string.Empty, Name = "root" }, true);
        return fileList;
    }

    public static async Task<System.IO.Stream> GetThumbnail(Runtime runtime, string id)
    {
        System.IO.Stream thresult = null;
        var thumbnailQuery = GetContentPath(id, true);
        runtime.Log.Info("Getting thumbnails for id: " + id);
        var result = await CallOneDrive<ThumbNails, OneDriveErrorResponse>(runtime, thumbnailQuery);
        if (result != null && result.Item1 != null && result.Item1.Items != null && result.Item1.Items.Count > 0)
        {
            var medium = result.Item1.Items[0].Medium == null ? null : result.Item1.Items[0].Medium.Url;
            if (!string.IsNullOrWhiteSpace(medium))
            {
                runtime.Log.Info("Downloading thumbnail from " + medium + ", id: " + id);
                thresult = await Http.MakeRequestForFile(medium, HttpMethod.Get, null, null, null);
            }
            else
            {
                runtime.Log.Warning("No medium thumbnail present for id: " + id);
            }
        }
        else
        {
            runtime.Log.Warning("No thumbnail present for id: " + id);
        }

        return thresult;
    }

    public static async Task<System.IO.Stream> GetFileContent(Runtime runtime, string id)
    {
        var fileQuery = GetContentPath(id, false);
        runtime.Log.Info("Downloading file from " + fileQuery + ", id: " + id);
        var fileStream = await CallOneDriveForFile(runtime, fileQuery);
        return fileStream;
    }

    public static async Task<string> DeleteFile(Runtime runtime, OneDriveItem deleteItem)
    {
        runtime.Log.Info("Deleting " + deleteItem.FullPath);
        string response = null;
        var deleteUrl = string.Format(AppConfiguration.OneDriveFileDeleteFormat, AppConfiguration.OneDriveBaseUri, deleteItem.Id);
        var result = await Http.MakeRequest<string, OneDriveErrorResponse>(deleteUrl, HttpMethod.Delete, OAuthHeaders(runtime), null, null);
        if (result.Item2 != null)
        {
            //// Error condition
        }
        else
        {
            response = result.Item1;
        }

        runtime.Log.Info("Deleted successfully " + deleteItem.FullPath);
        return response;
    }

    private static string GetContentPath(string id, bool isThumbnail)
    {
        return isThumbnail ? string.Format(AppConfiguration.OneDriveFileThumbnailFormat, AppConfiguration.OneDriveBaseUri, id) :
            string.Format(AppConfiguration.OneDriveFileContentFormat, AppConfiguration.OneDriveBaseUri, id);
    }

    private static async Task<List<OneDriveItem>> GetFolderItems(Runtime runtime, string folderId, bool isRoot)
    {
        var folderUrl = GetFolderPath(folderId, isRoot);
        var items = new List<OneDriveItem>();
        var result = await CallOneDrive<DriveItems, OneDriveErrorResponse>(runtime, folderUrl);
        if (result.Item1 == null)
        {
            //// Error condition
        }
        else
        {
            items.AddRange(result.Item1.Items);
            var nextLink = result.Item1.NextLink;
            while(!string.IsNullOrWhiteSpace(nextLink))
            {
                var nextResult = await CallOneDrive<DriveItems, OneDriveErrorResponse>(runtime, nextLink);
                if (nextResult.Item1 == null)
                {
                    //// Error condition
                }
                else
                {
                    items.AddRange(nextResult.Item1.Items);
                    nextLink = nextResult.Item1.NextLink;
                }
            }
        }

        return items;
    }

    private static async Task<List<OneDriveItem>> GetFilesAndFolders(Runtime runtime, OneDriveItem folder, bool isRoot)
    {
        var files = new List<OneDriveItem>();
        var items = await GetFolderItems(runtime, folder.Id, isRoot);
        var foldersAndFiles = SeparateFoldersAndFiles(items);
        if (foldersAndFiles != null)
        {
            if (foldersAndFiles.Item2 != null && foldersAndFiles.Item2.Any())
            {
                files.AddRange(foldersAndFiles.Item2);
                if (!isRoot)
                {
                    FolderCount++;
                }
                runtime.Log.Info(FolderCount + ". Processing Folder: " + folder.Name + ", number of files: " + foldersAndFiles.Item2.Count());
            }

            if (foldersAndFiles.Item1 != null && foldersAndFiles.Item1.Any())
            {
                var subFiles = await ProcessFolders(foldersAndFiles.Item1, runtime);
                files.AddRange(subFiles);
            }
        }
        return files;
    }

    private static async Task<List<OneDriveItem>> GetFilesFromFolders(IEnumerable<OneDriveItem> itemList, Runtime runtime)
    {
        var files = new List<OneDriveItem>();
        var fileItemTaskList = new List<Task<List<OneDriveItem>>>();
        itemList.ForEach(f => {
            fileItemTaskList.Add(GetFilesAndFolders(runtime, f, false));
        });

        await Task.WhenAll(fileItemTaskList);
        fileItemTaskList.ForEach(t => {
            files.AddRange(t.Result);
        });
        
        return files;
    }

    private static async Task<List<OneDriveItem>> ProcessFolders(List<OneDriveItem> folders, Runtime runtime)
    {
        var files = new List<OneDriveItem>();
        if (folders != null && folders.Any())
        {
            var taskList = new List<Task<List<OneDriveItem>>>();
            var count = folders.Count;
            for (var i = 0; i < count; i += OneDriveBatchSize)
            {
                var foldersBatch = folders.Skip(i).Take(OneDriveBatchSize > count ? count : OneDriveBatchSize);
                taskList.Add(GetFilesFromFolders(foldersBatch, runtime));
            }
            runtime.Log.Info("Folders Count: " + count + ", Total threads: " + taskList.Count + ", BatchSize: " + OneDriveBatchSize);
            await Task.WhenAll(taskList);
            taskList.ForEach(t => files.AddRange(t.Result));
        }

        return files;
    }

    private static Tuple<List<OneDriveItem>, List<OneDriveItem>> SeparateFoldersAndFiles(List<OneDriveItem> itemList)
    {
        if (itemList != null && itemList.Any())
        {
            var folders = new List<OneDriveItem>();
            var files = new List<OneDriveItem>();
            itemList.ForEach(f => {
                if (f.IsFolder)
                {
                    folders.Add(f);
                }
                else
                {
                    files.Add(f);
                }
            });
            return Tuple.Create(folders, files);
        }

        return null;
    }

    private static async Task<Tuple<TResponse, TError>> CallOneDrive<TResponse, TError>(Runtime runtime, string queryUrl)
    {       
        return await Http.MakeRequest<TResponse, TError>(queryUrl, HttpMethod.Get, OAuthHeaders(runtime), null, null);        
    }

   private static async Task<Stream> CallOneDriveForFile(Runtime runtime, string queryUrl)
   {  
        return await Http.MakeRequestForFile(queryUrl, HttpMethod.Get, OAuthHeaders(runtime), null, null);
    }

    private static Dictionary<string, string> OAuthHeaders(Runtime runtime)
    {
        var authHeader = OAuth.GetAccessoken(runtime);
        var headers = new Dictionary<string, string>();
        headers.Add("Authorization", "Bearer " + authHeader);
        return headers;
    }

    private static string GetFolderPath(string id, bool isRoot)
    {
        return isRoot ?
                 string.Format(AppConfiguration.OneDriveRootFormat, AppConfiguration.OneDriveBaseUri) :
                 string.Format(AppConfiguration.OneDriveFolderFormat, AppConfiguration.OneDriveBaseUri, id);
    }

   
}