#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Web"

#load "./../libs/models/keys.csx"
#load "./../libs/common/blob.csx"
#load "./../libs/models/runtime.csx"
#load "./../libs/models/fileinfo.csx"
#load "./../libs/models/cachefilelist.csx"
#load "./../libs/common/extension.csx"

using System;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Configuration;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

public static CacheResult CachedResult;
public static async Task<HttpResponseMessage> Run(HttpRequestMessage request,     
    CloudTable fileInfoTable,
    TraceWriter log)
{
     var runtime = new Runtime
    {
        Request = request,
        Log = log,      
      
    };

    var requestParams = HttpUtility.ParseQueryString(request.RequestUri.Query);
    var skip = requestParams.Get("skip");
    var top = requestParams.Get("top");
    var orderBy = requestParams.Get("orderby");
    var start = DateTime.UtcNow;
    log.Info("Start : " + start);

    if (CachedResult == null) 
    {           
        // Construct the query operation for all customer entities where PartitionKey="Smith".
        var fileQuery = new TableQuery<FileInfo>().Where(TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "DriveFiles"),
            TableOperators.And,
            TableQuery.GenerateFilterConditionForBool("Blobed", QueryComparisons.Equal, true)));
        var photoQuery = new TableQuery<PhotoInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PhotoFiles"));
        var videoQuery = new TableQuery<VideoInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "VideoFiles"));
        var taskLists = new List<Task>();
        var existingFiles = Task.Run(() => RunWithInstrumentation(()=> fileInfoTable.ExecuteQuery(fileQuery).Select(f => new { f.Id, f.Extension, f.FullPath, f.MimeType, f.LastModifiedBy, f.LastModified, f.Size, f.Type, f.Name }), "FileQuery", log));       
        taskLists.Add(existingFiles);
        var pFiles = Task.Run(() => RunWithInstrumentation(()=> fileInfoTable.ExecuteQuery(photoQuery).Select(f => new { f.Id, f.CameraMake, f.CameraModel, f.FNumber, f.FocalLength, f.Height, f.Iso, f.TakenDateTime, f.Width }), "PhotoQuery", log));               
        taskLists.Add(pFiles);
        var vFiles = Task.Run(() => RunWithInstrumentation(()=> fileInfoTable.ExecuteQuery(videoQuery).Select(f => new { f.Id, f.Height, f.Duration, f.Width }), "VideoQuery", log));
        taskLists.Add(vFiles);
        var keys = new Keys();
        var thumbToken = Task.Run(() => RunWithInstrumentation(() => BlobDrive.GetKey(runtime, true), "ThumbToken", log));
        taskLists.Add(thumbToken);
        var driveToken = Task.Run(() => RunWithInstrumentation(() => BlobDrive.GetKey(runtime, false), "DriveToken", log));
        taskLists.Add(driveToken);
        var thumbUri = Task.Run(() => RunWithInstrumentation(() => BlobDrive.GetUri(runtime, true), "ThumbUri", log));
        taskLists.Add(thumbUri);
        var driveUri = Task.Run(() => RunWithInstrumentation(() => BlobDrive.GetUri(runtime, false), "DriveUri", log));
        taskLists.Add(driveUri);
        /*
        var tablePolicy = new SharedAccessTablePolicy()
        {
            Permissions = SharedAccessTablePermissions.Query,
            SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1)
        };
        var listToken = Task.Run(() => { return fileInfoTable.GetSharedAccessSignature(tablePolicy); });
        taskLists.Add(listToken);
        */
        await Task.WhenAll(taskLists);
        var processTaskLists = new List<Task>();
        var orderTask = Task.Run(() => RunWithInstrumentation(() => existingFiles.Result.ToList().OrderByDescending(o => o.LastModified), "FileList_Execute", log));
        processTaskLists.Add(orderTask);
        var photoTask = Task.Run(() => RunWithInstrumentation(() => pFiles.Result.ToDictionary(p => p.Id, p => (object)p), "PhotoFiles_Execute", log));
        processTaskLists.Add(photoTask);
        var videoTask = Task.Run(() => RunWithInstrumentation(() => vFiles.Result.ToDictionary(v => v.Id, v => (object)v), "VideoFiles_Execute", log));
        processTaskLists.Add(photoTask);
        await Task.WhenAll(processTaskLists);
        //// collect the results    
        IEnumerable<dynamic> orderedFiles = orderTask.Result;
        var allPhotofiles = photoTask.Result;
        var allVideofiles = videoTask.Result;

        CachedResult = new CacheResult();
        CachedResult.AllFiles = orderedFiles;
        CachedResult.AllPhotofiles = allPhotofiles;
        CachedResult.AllVideofiles = allVideofiles;
        CachedResult.ThumbToken = thumbToken.Result;
        CachedResult.ThumbUri = thumbUri.Result;
        CachedResult.DriveToken = driveToken.Result;
        CachedResult.DriveUri = driveUri.Result;
        CachedResult.TotalCount = orderedFiles.Count();
    }
   
    // collect the results    
    IEnumerable<dynamic> corderedFiles = CachedResult.AllFiles;
    var callPhotofiles = CachedResult.AllPhotofiles;
    var callVideofiles = CachedResult.AllVideofiles;

    if (!string.IsNullOrWhiteSpace(orderBy))
    {
        if (orderBy.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
        {
            // orderedFiles = existingFiles.Result.OrderByDescending(orderBy);
        }
        else
        {
          //  orderedFiles = existingFiles.Result.OrderBy(orderBy);
        }
       
    }
    int skipValue = 0;
    if (!string.IsNullOrWhiteSpace(skip) && Int32.TryParse(skip, out skipValue))
    {
        corderedFiles = corderedFiles.Skip(skipValue);
    }
    int topValue = 0;
    if (!string.IsNullOrWhiteSpace(top) && Int32.TryParse(top, out topValue))
    {
        corderedFiles = corderedFiles.Take(topValue);
    }

    var selectedPhotofiles = new ConcurrentBag<dynamic>();
    var selectedVideofiles = new ConcurrentBag<dynamic>();
    Parallel.ForEach(corderedFiles, (of) => {
        if (of.Type == "Photo" && callPhotofiles.ContainsKey(of.Id))
        {
           selectedPhotofiles.Add(callPhotofiles[of.Id]);            
        }

        if (of.Type == "Video" && callVideofiles.ContainsKey(of.Id))
        {           
            selectedVideofiles.Add(callVideofiles[of.Id]);            
        } 
    });   
   
    var elapsed = DateTime.UtcNow - start;
    log.Info("Total Duration : " + elapsed);
    var result = new { 
                Duration = elapsed, 
                Url = CachedResult.DriveUri, 
                ThumbUrl = CachedResult.ThumbUri, 
                DriveToken =  CachedResult.DriveToken, 
                ThumbToken = CachedResult.ThumbToken, 
                Files = corderedFiles, 
                Photos = selectedPhotofiles, 
                Videos = selectedVideofiles,
                TotalCount = CachedResult.TotalCount
            };   
    return request.CreateResponse(HttpStatusCode.OK, result, new JsonMediaTypeFormatter());
}
