#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "System.Web"

#load "./../libs/models/keys.csx"
#load "./../libs/common/blob.csx"
#load "./../libs/models/runtime.csx"
#load "./../libs/models/fileinfo.csx"

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
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

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
    // Construct the query operation for all customer entities where PartitionKey="Smith".
    TableQuery<FileInfo> fileQuery = new TableQuery<FileInfo>().Where(TableQuery.CombineFilters(
        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "DriveFiles"),
        TableOperators.And,
        TableQuery.GenerateFilterConditionForBool("Blobed", QueryComparisons.Equal, true)));
    TableQuery<PhotoInfo> photoQuery = new TableQuery<PhotoInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PhotoFiles"));
    TableQuery<VideoInfo> videoQuery = new TableQuery<VideoInfo>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "VideoFiles"));
    var taskLists = new List<Task>();
    var existingFiles = Task.Run(() => { return fileInfoTable.ExecuteQuery(fileQuery).Select(f => new { f.Id, f.Extension, f.FullPath, f.MimeType, f.LastModifiedBy, f.LastModified, f.Size, f.Type, f.Name }); });
    taskLists.Add(existingFiles);
    var pFiles = Task.Run(() => { return fileInfoTable.ExecuteQuery(photoQuery).Select(f => new { f.Id, f.CameraMake, f.CameraModel, f.FNumber, f.FocalLength, f.Height, f.Iso, f.TakenDateTime, f.Width }); });
    taskLists.Add(pFiles);
    var vFiles = Task.Run(() => { return fileInfoTable.ExecuteQuery(videoQuery).Select(f => new { f.Id, f.Height, f.Duration, f.Width }); });
    taskLists.Add(vFiles);
    var keys = new Keys();
    var thumbToken = Task.Run(() => { return BlobDrive.GetKey(runtime, true); });
    taskLists.Add(thumbToken);
    var driveToken = Task.Run(() => { return BlobDrive.GetKey(runtime, false); });
    taskLists.Add(driveToken);
    var thumbUri = Task.Run(() => { return BlobDrive.GetUri(runtime, true); });
    taskLists.Add(thumbUri);
    var driveUri = Task.Run(() => { return BlobDrive.GetUri(runtime, false); });
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
    IEnumerable<dynamic> orderedFiles = existingFiles.Result.ToList().OrderByDescending(o => o.LastModified);
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
        orderedFiles = orderedFiles.Skip(skipValue);
    }
    int topValue = 0;
    if (!string.IsNullOrWhiteSpace(top) && Int32.TryParse(top, out topValue))
    {
        orderedFiles = orderedFiles.Take(topValue);
    }
    
    var selectedPhotofiles = new List<dynamic>();
    var selectedVideofiles = new List<dynamic>();
    foreach (var of in orderedFiles)
    {
        if (of.Type == "Photo")
        {
            var pf = pFiles.Result.FirstOrDefault(p => p.Id == of.Id);
            if (pf != null)
            {
                selectedPhotofiles.Add(pf);
            }
        }

        if (of.Type == "Video")
        {
            var vf = vFiles.Result.FirstOrDefault(v => v.Id == of.Id);
            if (vf != null)
            {
                selectedVideofiles.Add(vf);
            }
        }            
    }
   
    var elapsed = DateTime.UtcNow - start;
    log.Info("Duration : " + elapsed);
    var result = new { Duration = elapsed, Url = driveUri.Result, ThumbUrl = thumbUri.Result, DriveToken = driveToken.Result, ThumbToken = thumbToken.Result, Files = orderedFiles, Photos = selectedPhotofiles, Videos = selectedVideofiles };   
    return request.CreateResponse(HttpStatusCode.OK, result, new JsonMediaTypeFormatter());
}
